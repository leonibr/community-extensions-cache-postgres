// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Community.Microsoft.Extensions.Caching.PostgreSql
{
    internal sealed class DatabaseOperations : IDatabaseOperations
    {
        private readonly ILogger<DatabaseOperations> _logger;
        private readonly bool _updateOnGetCacheItem;
        private readonly bool _readOnlyMode;

        public DatabaseOperations(IOptions<PostgreSqlCacheOptions> options, ILogger<DatabaseOperations> logger)
        {
            var cacheOptions = options.Value;

            if (string.IsNullOrEmpty(cacheOptions.ConnectionString) && cacheOptions.DataSourceFactory is null)
            {
                throw new ArgumentException(
                    $"Either {nameof(PostgreSqlCacheOptions.ConnectionString)} or {nameof(PostgreSqlCacheOptions.DataSourceFactory)} must be set.");
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

            ConnectionFactory = cacheOptions.DataSourceFactory != null
                ? () => cacheOptions.DataSourceFactory.Invoke().CreateConnection()
                : new Func<NpgsqlConnection>(() => new NpgsqlConnection(cacheOptions.ConnectionString));

            SystemClock = cacheOptions.SystemClock;

            SqlCommands = new SqlCommands(cacheOptions.SchemaName, cacheOptions.TableName);

            this._logger = logger;
            this._updateOnGetCacheItem = cacheOptions.UpdateOnGetCacheItem;
            this._readOnlyMode = cacheOptions.ReadOnlyMode;
            if (cacheOptions.CreateInfrastructure)
            {
                CreateSchemaAndTableIfNotExist();
            }
        }

        private SqlCommands SqlCommands { get; }

        private Func<NpgsqlConnection> ConnectionFactory { get; }

        private ISystemClock SystemClock { get; }

        private void CreateSchemaAndTableIfNotExist()
        {
            if (_readOnlyMode)
            {
                _logger.LogDebug("CreateTableIfNotExist skipped due to ReadOnlyMode");
                return;
            }

            using (var connection = ConnectionFactory())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    using var createSchemaAndTable = new NpgsqlCommand(
                        SqlCommands.CreateSchemaAndTableSql,
                        connection,
                        transaction);
                    createSchemaAndTable.ExecuteNonQuery();

                    transaction.Commit();
                }
            }

            _logger.LogDebug("CreateTableIfNotExist executed");
        }

        public void DeleteCacheItem(string key)
        {
            if (_readOnlyMode)
            {
                _logger.LogDebug("DeleteCacheItem skipped due to ReadOnlyMode");
                return;
            }

            using var connection = ConnectionFactory();
            connection.Open();

            using var deleteCacheItem = new NpgsqlCommand(SqlCommands.DeleteCacheItemSql, connection);
            deleteCacheItem.Parameters.Add(new NpgsqlParameter<string>(nameof(ItemIdOnly.Id), key));
            deleteCacheItem.ExecuteNonQuery();

            _logger.LogDebug($"Cache key deleted: {key}");
        }

        public async Task DeleteCacheItemAsync(string key, CancellationToken cancellationToken)
        {
            if (_readOnlyMode)
            {
                _logger.LogDebug("DeleteCacheItem skipped due to ReadOnlyMode");
                return;
            }
            await using var connection = ConnectionFactory();
            await connection.OpenAsync(cancellationToken);

            await using var deleteCacheItem = new NpgsqlCommand(SqlCommands.DeleteCacheItemSql, connection);
            deleteCacheItem.Parameters.Add(new NpgsqlParameter<string>(nameof(ItemIdOnly.Id), key));
            await deleteCacheItem.ExecuteNonQueryAsync(cancellationToken);

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
            if (_readOnlyMode)
                return;

            var utcNow = SystemClock.UtcNow;

            await using var connection = ConnectionFactory();
            await connection.OpenAsync(cancellationToken);

            await using var deleteExpiredCache = new NpgsqlCommand(SqlCommands.DeleteExpiredCacheSql, connection);
            deleteExpiredCache.Parameters.Add(new NpgsqlParameter<DateTimeOffset>(nameof(CurrentUtcNow.UtcNow), utcNow));
            await deleteExpiredCache.ExecuteNonQueryAsync(cancellationToken);
        }

        public void SetCacheItem(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            if (_readOnlyMode)
                return;

            var utcNow = SystemClock.UtcNow;

            var absoluteExpiration = GetAbsoluteExpiration(utcNow, options);
            ValidateOptions(options.SlidingExpiration, absoluteExpiration);

            using var connection = ConnectionFactory();
            connection.Open();

            var expiresAtTime = options.SlidingExpiration == null
                ? absoluteExpiration!.Value
                : utcNow.Add(options.SlidingExpiration.Value);

            using var setCache = new NpgsqlCommand(SqlCommands.SetCacheSql, connection);
            setCache.Parameters.Add(new NpgsqlParameter<string>(nameof(ItemFull.Id), key));
            setCache.Parameters.Add(new NpgsqlParameter<byte[]>(nameof(ItemFull.Value), value));
            setCache.Parameters.Add(new NpgsqlParameter<DateTimeOffset>(nameof(ItemFull.ExpiresAtTime), expiresAtTime));
            setCache.Parameters.Add(new NpgsqlParameter<double?>(nameof(ItemFull.SlidingExpirationInSeconds), options.SlidingExpiration?.TotalSeconds));
            setCache.Parameters.Add(new NpgsqlParameter<DateTimeOffset?>(nameof(ItemFull.AbsoluteExpiration), absoluteExpiration));
            setCache.ExecuteNonQuery();
        }

        public async Task SetCacheItemAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken cancellationToken)
        {
            if (_readOnlyMode)
                return;

            var utcNow = SystemClock.UtcNow;

            var absoluteExpiration = GetAbsoluteExpiration(utcNow, options);
            ValidateOptions(options.SlidingExpiration, absoluteExpiration);

            await using var connection = ConnectionFactory();
            await connection.OpenAsync(cancellationToken);

            var expiresAtTime = options.SlidingExpiration == null
                ? absoluteExpiration!.Value
                : utcNow.Add(options.SlidingExpiration.Value);

            await using var setCache = new NpgsqlCommand(SqlCommands.SetCacheSql, connection);
            setCache.Parameters.Add(new NpgsqlParameter<string>(nameof(ItemFull.Id), key));
            setCache.Parameters.Add(new NpgsqlParameter<byte[]>(nameof(ItemFull.Value), value));
            setCache.Parameters.Add(new NpgsqlParameter<DateTimeOffset>(nameof(ItemFull.ExpiresAtTime), expiresAtTime));
            setCache.Parameters.Add(new NpgsqlParameter<double?>(nameof(ItemFull.SlidingExpirationInSeconds), options.SlidingExpiration?.TotalSeconds));
            setCache.Parameters.Add(new NpgsqlParameter<DateTimeOffset?>(nameof(ItemFull.AbsoluteExpiration), absoluteExpiration));
            await setCache.ExecuteNonQueryAsync(cancellationToken);
        }

        private byte[] GetCacheItem(string key, bool includeValue)
        {
            var utcNow = SystemClock.UtcNow;
            byte[] value = null;

            using var connection = ConnectionFactory();
            connection.Open();

            if (!_readOnlyMode && (_updateOnGetCacheItem || !includeValue))
            {
                using var updateCacheItem = new NpgsqlCommand(SqlCommands.UpdateCacheItemSql, connection);
                updateCacheItem.Parameters.Add(new NpgsqlParameter<string>(nameof(ItemIdUtcNow.Id), key));
                updateCacheItem.Parameters.Add(new NpgsqlParameter<DateTimeOffset>(nameof(ItemIdUtcNow.UtcNow), utcNow));
                updateCacheItem.ExecuteNonQuery();
            }

            if (includeValue)
            {
                using var getCacheItem = new NpgsqlCommand(SqlCommands.GetCacheItemSql, connection);
                getCacheItem.Parameters.Add(new NpgsqlParameter<string>(nameof(ItemIdUtcNow.Id), key));
                getCacheItem.Parameters.Add(new NpgsqlParameter<DateTimeOffset>(nameof(ItemIdUtcNow.UtcNow), utcNow));
                value = getCacheItem.ExecuteScalar() as byte[];
            }

            return value;
        }

        private async Task<byte[]> GetCacheItemAsync(string key, bool includeValue, CancellationToken cancellationToken)
        {
            var utcNow = SystemClock.UtcNow;
            byte[] value = null;

            await using var connection = ConnectionFactory();
            await connection.OpenAsync(cancellationToken);

            if (!_readOnlyMode && (_updateOnGetCacheItem || !includeValue))
            {
                await using var updateCacheItem = new NpgsqlCommand(SqlCommands.UpdateCacheItemSql, connection);
                updateCacheItem.Parameters.Add(new NpgsqlParameter<string>(nameof(ItemIdUtcNow.Id), key));
                updateCacheItem.Parameters.Add(new NpgsqlParameter<DateTimeOffset>(nameof(ItemIdUtcNow.UtcNow), utcNow));
                await updateCacheItem.ExecuteNonQueryAsync(cancellationToken);
            }

            if (includeValue)
            {
                await using var getCacheItem = new NpgsqlCommand(SqlCommands.GetCacheItemSql, connection);
                getCacheItem.Parameters.Add(new NpgsqlParameter<string>(nameof(ItemIdUtcNow.Id), key));
                getCacheItem.Parameters.Add(new NpgsqlParameter<DateTimeOffset>(nameof(ItemIdUtcNow.UtcNow), utcNow));
                value = await getCacheItem.ExecuteScalarAsync(cancellationToken) as byte[];
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