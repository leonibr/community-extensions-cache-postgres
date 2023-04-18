// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Npgsql;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Internal;
using System.Threading;
using Dapper;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace Community.Microsoft.Extensions.Caching.PostgreSql
{
    internal sealed class DatabaseOperations : IDatabaseOperations
    {
        private readonly ILogger<DatabaseOperations> _logger;
        private readonly bool _updateOnGetCacheItem;

        public DatabaseOperations(IOptions<PostgreSqlCacheOptions> options, ILogger<DatabaseOperations> logger)
        {
            var cacheOptions = options.Value;

            if (string.IsNullOrEmpty(cacheOptions.ConnectionString))
            {
                throw new ArgumentException(
                    $"{nameof(PostgreSqlCacheOptions.ConnectionString)} cannot be empty or null.");
            }
            if (string.IsNullOrEmpty(cacheOptions.SchemaName))
            {
                throw new ArgumentException(
                    $"{nameof(PostgreSqlCacheOptions.SchemaName)} cannot be empty or null.");
            }
            if (string.IsNullOrEmpty(cacheOptions.TableName))
            {
                throw new ArgumentException(
                    $"{nameof(PostgreSqlCacheOptions.TableName)} cannot be empty or null.");
            }

            ConnectionString = cacheOptions.ConnectionString;
            SystemClock = cacheOptions.SystemClock;

            SqlCommands = new SqlCommands(cacheOptions.SchemaName, cacheOptions.TableName);

            this._logger = logger;
            this._updateOnGetCacheItem = !cacheOptions.DisableUpdateOnGetCacheItem;
            if (cacheOptions.CreateInfrastructure)
            {
                CreateSchemaAndTableIfNotExist();
            }
        }

        private SqlCommands SqlCommands { get; }

        private string ConnectionString { get; }

        private ISystemClock SystemClock { get; }

        private void CreateSchemaAndTableIfNotExist()
        {
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var createSchemaAndTable = new CommandDefinition(
                        SqlCommands.CreateSchemaAndTableSql,
                        transaction: transaction);
                    connection.Execute(createSchemaAndTable);

                    transaction.Commit();
                }
            }

            _logger.LogDebug("CreateTableIfNotExist executed");
        }

        public void DeleteCacheItem(string key)
        {
            using var connection = new NpgsqlConnection(ConnectionString);

            var deleteCacheItem = new CommandDefinition(
                SqlCommands.DeleteCacheItemSql,
                new ItemIdOnly { Id = key });
            connection.Execute(deleteCacheItem);

            _logger.LogDebug($"Cache key deleted: {key}");
        }

        public async Task DeleteCacheItemAsync(string key, CancellationToken cancellationToken)
        {
            await using var connection = new NpgsqlConnection(ConnectionString);

            var deleteCacheItem = new CommandDefinition(
                SqlCommands.DeleteCacheItemSql,
                new ItemIdOnly { Id = key },
                cancellationToken: cancellationToken);
            await connection.ExecuteAsync(deleteCacheItem);

            _logger.LogDebug($"Cache key deleted: {key}");
        }

        public byte[] GetCacheItem(string key) =>
            GetCacheItem(key, includeValue: true);

        public async Task<byte[]> GetCacheItemAsync(string key, CancellationToken cancellationToken) =>
            await GetCacheItemAsync(key, includeValue: true, cancellationToken);

        public void RefreshCacheItem(string key) =>
            GetCacheItem(key, includeValue: false);

        public async Task RefreshCacheItemAsync(string key, CancellationToken cancellationToken) =>
            await GetCacheItemAsync(key, includeValue: false, cancellationToken);


        public async Task DeleteExpiredCacheItemsAsync(CancellationToken cancellationToken)
        {
            var utcNow = SystemClock.UtcNow;

            await using var connection = new NpgsqlConnection(ConnectionString);

            var deleteExpiredCache = new CommandDefinition(
                SqlCommands.DeleteExpiredCacheSql,
                new CurrentUtcNow { UtcNow = utcNow },
                cancellationToken: cancellationToken);
            await connection.ExecuteAsync(deleteExpiredCache);
        }

        public void SetCacheItem(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            var utcNow = SystemClock.UtcNow;

            var absoluteExpiration = GetAbsoluteExpiration(utcNow, options);
            ValidateOptions(options.SlidingExpiration, absoluteExpiration);

            using var connection = new NpgsqlConnection(ConnectionString);

            var expiresAtTime = options.SlidingExpiration == null
                ? absoluteExpiration!.Value
                : utcNow.Add(options.SlidingExpiration.Value);

            var setCache = new CommandDefinition(
                SqlCommands.SetCacheSql,
                new ItemFull
                {
                    Id = key,
                    Value = value,
                    ExpiresAtTime = expiresAtTime,
                    SlidingExpirationInSeconds = options.SlidingExpiration?.TotalSeconds,
                    AbsoluteExpiration = absoluteExpiration
                });

            connection.Execute(setCache);
        }

        public async Task SetCacheItemAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken cancellationToken)
        {
            var utcNow = SystemClock.UtcNow;

            var absoluteExpiration = GetAbsoluteExpiration(utcNow, options);
            ValidateOptions(options.SlidingExpiration, absoluteExpiration);

            await using var connection = new NpgsqlConnection(ConnectionString);

            var expiresAtTime = options.SlidingExpiration == null
                ? absoluteExpiration!.Value
                : utcNow.Add(options.SlidingExpiration.Value);

            var setCache = new CommandDefinition(
                SqlCommands.SetCacheSql,
                new ItemFull
                {
                    Id = key,
                    Value = value,
                    ExpiresAtTime = expiresAtTime,
                    SlidingExpirationInSeconds = options.SlidingExpiration?.TotalSeconds,
                    AbsoluteExpiration = absoluteExpiration
                },
                cancellationToken: cancellationToken);

            await connection.ExecuteAsync(setCache);
        }

        private byte[] GetCacheItem(string key, bool includeValue)
        {
            var utcNow = SystemClock.UtcNow;
            byte[] value = null;

            using var connection = new NpgsqlConnection(ConnectionString);

            if (_updateOnGetCacheItem)
            {
                var updateCacheItem = new CommandDefinition(
                    SqlCommands.UpdateCacheItemSql,
                    new ItemIdUtcNow { Id = key, UtcNow = utcNow });
                connection.Execute(updateCacheItem);
            }

            if (includeValue)
            {
                var getCacheItem = new CommandDefinition(
                    SqlCommands.GetCacheItemSql,
                    new ItemIdUtcNow { Id = key, UtcNow = utcNow });
                value = connection.QueryFirstOrDefault<byte[]>(getCacheItem);
            }

            return value;
        }

        private async Task<byte[]> GetCacheItemAsync(string key, bool includeValue, CancellationToken cancellationToken)
        {
            var utcNow = SystemClock.UtcNow;
            byte[] value = null;

            await using var connection = new NpgsqlConnection(ConnectionString);

            if (_updateOnGetCacheItem)
            {
                var updateCacheItem = new CommandDefinition(
                    SqlCommands.UpdateCacheItemSql,
                    new ItemIdUtcNow { Id = key, UtcNow = utcNow },
                    cancellationToken: cancellationToken);
                await connection.ExecuteAsync(updateCacheItem);
            }

            if (includeValue)
            {
                var getCacheItem = new CommandDefinition(
                    SqlCommands.GetCacheItemSql,
                    new ItemIdUtcNow { Id = key, UtcNow = utcNow },
                    cancellationToken: cancellationToken);
                value = await connection.QueryFirstOrDefaultAsync<byte[]>(getCacheItem);
            }

            return value;
        }

        private DateTimeOffset? GetAbsoluteExpiration(DateTimeOffset utcNow, DistributedCacheEntryOptions options)
        {
            // calculate absolute expiration
            DateTimeOffset? absoluteExpiration = null;
            if (options.AbsoluteExpirationRelativeToNow.HasValue)
            {
                absoluteExpiration = utcNow.Add(options.AbsoluteExpirationRelativeToNow.Value);
            }
            else if (options.AbsoluteExpiration.HasValue)
            {
                if (options.AbsoluteExpiration.Value.ToUniversalTime() <= utcNow.ToUniversalTime())
                {
                    throw new InvalidOperationException("The absolute expiration value must be in the future.");
                }

                absoluteExpiration = options.AbsoluteExpiration.Value;
            }
            return absoluteExpiration;
        }

        private void ValidateOptions(TimeSpan? slidingExpiration, DateTimeOffset? absoluteExpiration)
        {
            if (!slidingExpiration.HasValue && !absoluteExpiration.HasValue)
            {
                throw new InvalidOperationException("Either absolute or sliding expiration needs " +
                    "to be provided.");
            }
        }
    }
}