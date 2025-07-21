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
using Polly;

namespace Community.Microsoft.Extensions.Caching.PostgreSql
{
    internal sealed class DatabaseOperations : IDatabaseOperations, IDisposable
    {
        private readonly ILogger<DatabaseOperations> _logger;
        private readonly bool _updateOnGetCacheItem;
        private readonly bool _readOnlyMode;
        private readonly ReloadableConnectionStringProvider _connectionStringProvider;
        private readonly PostgreSqlCacheOptions _options;
        private readonly IAsyncPolicy _resiliencePolicy;
        private readonly IPolicyFactory _policyFactory;

        public DatabaseOperations(IOptions<PostgreSqlCacheOptions> options, ILogger<DatabaseOperations> logger)
        {
            var cacheOptions = options.Value;

            if (string.IsNullOrEmpty(cacheOptions.ConnectionString) &&
                string.IsNullOrEmpty(cacheOptions.ConnectionStringKey) &&
                cacheOptions.DataSourceFactory is null)
            {
                throw new ArgumentException(
                    $"Either {nameof(PostgreSqlCacheOptions.ConnectionString)}, {nameof(PostgreSqlCacheOptions.ConnectionStringKey)}, or {nameof(PostgreSqlCacheOptions.DataSourceFactory)} must be set.");
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

            // Initialize connection string provider if using reloadable connection strings
            if (!string.IsNullOrEmpty(cacheOptions.ConnectionStringKey) &&
                cacheOptions.EnableConnectionStringReloading &&
                cacheOptions.Configuration != null)
            {
                _connectionStringProvider = new ReloadableConnectionStringProvider(
                    cacheOptions.Configuration,
                    cacheOptions.Logger ?? logger,
                    cacheOptions.ConnectionStringKey,
                    cacheOptions.ConnectionStringReloadInterval);
            }

            ConnectionFactory = cacheOptions.DataSourceFactory != null
                ? () => cacheOptions.DataSourceFactory.Invoke().CreateConnection()
                : new Func<NpgsqlConnection>(() => new NpgsqlConnection(GetConnectionString(cacheOptions)));

            SystemClock = cacheOptions.SystemClock;

            SqlCommands = new SqlCommands(cacheOptions.SchemaName, cacheOptions.TableName);

            this._logger = logger;
            this._updateOnGetCacheItem = cacheOptions.UpdateOnGetCacheItem;
            this._readOnlyMode = cacheOptions.ReadOnlyMode;
            this._options = cacheOptions;

            // Initialize resilience patterns if enabled
            if (cacheOptions.EnableResiliencePatterns)
            {
                cacheOptions.ValidateResilienceConfiguration();
                _policyFactory = new PolicyFactory(logger);
                _resiliencePolicy = _policyFactory.CreateResiliencePolicy(cacheOptions);
            }

            if (cacheOptions.CreateInfrastructure)
            {
                CreateSchemaAndTableIfNotExist();
            }
        }

        private string GetConnectionString(PostgreSqlCacheOptions options)
        {
            if (_connectionStringProvider != null)
            {
                return _connectionStringProvider.GetConnectionString();
            }
            return options.ConnectionString;
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
            if (_readOnlyMode)
            {
                _logger.LogDebug("DeleteCacheItem skipped due to ReadOnlyMode");
                return;
            }

            ExecuteWithResilience(() =>
            {
                using var connection = ConnectionFactory();
                var deleteCacheItem = new CommandDefinition(
                    SqlCommands.DeleteCacheItemSql,
                    new ItemIdOnly { Id = key });
                connection.Execute(deleteCacheItem);
                _logger.LogDebug($"Cache key deleted: {key}");
            }, "DeleteCacheItem", key);
        }

        public async Task DeleteCacheItemAsync(string key, CancellationToken cancellationToken)
        {
            if (_readOnlyMode)
            {
                _logger.LogDebug("DeleteCacheItem skipped due to ReadOnlyMode");
                return;
            }

            await ExecuteWithResilienceAsync(async () =>
            {
                await using var connection = ConnectionFactory();
                var deleteCacheItem = new CommandDefinition(
                    SqlCommands.DeleteCacheItemSql,
                    new ItemIdOnly { Id = key },
                    cancellationToken: cancellationToken);
                await connection.ExecuteAsync(deleteCacheItem);
                _logger.LogDebug($"Cache key deleted: {key}");
            }, "DeleteCacheItemAsync", key);
        }

        public byte[] GetCacheItem(string key) =>
            ExecuteWithResilience(() => GetCacheItem(key, includeValue: true), null, "GetCacheItem", key);

        public async Task<byte[]> GetCacheItemAsync(string key, CancellationToken cancellationToken) =>
            await ExecuteWithResilienceAsync(() => GetCacheItemAsync(key, includeValue: true, cancellationToken), null, "GetCacheItemAsync", key);

        public void RefreshCacheItem(string key) =>
            ExecuteWithResilience(() => GetCacheItem(key, includeValue: false), null, "RefreshCacheItem", key);

        public async Task RefreshCacheItemAsync(string key, CancellationToken cancellationToken) =>
            await ExecuteWithResilienceAsync(() => GetCacheItemAsync(key, includeValue: false, cancellationToken), null, "RefreshCacheItemAsync", key);


        public async Task DeleteExpiredCacheItemsAsync(CancellationToken cancellationToken)
        {
            if (_readOnlyMode)
                return;

            await ExecuteWithResilienceAsync(async () =>
            {
                var utcNow = SystemClock.UtcNow;
                await using var connection = ConnectionFactory();

                var deleteExpiredCache = new CommandDefinition(
                    SqlCommands.DeleteExpiredCacheSql,
                    new CurrentUtcNow { UtcNow = utcNow },
                    cancellationToken: cancellationToken);
                await connection.ExecuteAsync(deleteExpiredCache);
            }, "DeleteExpiredCacheItemsAsync");
        }

        public void SetCacheItem(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            if (_readOnlyMode)
                return;

            ExecuteWithResilience(() =>
            {
                var utcNow = SystemClock.UtcNow;

                var absoluteExpiration = GetAbsoluteExpiration(utcNow, options);

                using var connection = ConnectionFactory();

                var expiresAtTime = GetExpiresAtTime(utcNow, absoluteExpiration, options.SlidingExpiration);

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
            }, "SetCacheItem", key);
        }

        public async Task SetCacheItemAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken cancellationToken)
        {
            if (_readOnlyMode)
                return;

            await ExecuteWithResilienceAsync(async () =>
            {
                var utcNow = SystemClock.UtcNow;

                var absoluteExpiration = GetAbsoluteExpiration(utcNow, options);

                await using var connection = ConnectionFactory();

                var expiresAtTime = GetExpiresAtTime(utcNow, absoluteExpiration, options.SlidingExpiration);

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
            }, "SetCacheItemAsync", key);
        }

        private byte[] GetCacheItem(string key, bool includeValue)
        {
            var utcNow = SystemClock.UtcNow;
            byte[] value = null;

            using var connection = ConnectionFactory();

            if (!_readOnlyMode && (_updateOnGetCacheItem || !includeValue))
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

            await using var connection = ConnectionFactory();

            if (!_readOnlyMode && (_updateOnGetCacheItem || !includeValue))
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

        private DateTimeOffset? GetExpiresAtTime(DateTimeOffset utcNow, DateTimeOffset? absoluteExpiration, TimeSpan? slidingExpiration)
        {
            if (!slidingExpiration.HasValue)
            {
                return absoluteExpiration;
            }
            
            var expiration = utcNow.Add(slidingExpiration.Value);

            // Pick whichever comes first: the absolute or the sliding deadline
            return absoluteExpiration.HasValue && expiration > absoluteExpiration.Value ? absoluteExpiration : expiration;
        }

        /// <summary>
        /// Executes an async operation with resilience policies if enabled.
        /// </summary>
        private async Task<T> ExecuteWithResilienceAsync<T>(Func<Task<T>> operation, T defaultValue, string operationName, string key = null)
        {
            if (_resiliencePolicy != null)
            {
                try
                {
                    var context = new Context(operationName);
                    if (!string.IsNullOrEmpty(key))
                    {
                        context["Key"] = key;
                    }

                    return await _resiliencePolicy.ExecuteAsync(async (ctx) => await operation(), context);
                }
                catch (Exception ex) when (IsConnectionFailure(ex))
                {
                    _logger.Log(_options.ConnectionFailureLogLevel, ex,
                        "Database connection failed for {Operation} operation. Key: {Key}", operationName, key);
                    return defaultValue;
                }
            }
            else
            {
                // When resilience patterns are disabled, execute operation directly without catching exceptions
                return await operation();
            }
        }

        /// <summary>
        /// Executes a sync operation with resilience policies if enabled.
        /// </summary>
        private T ExecuteWithResilience<T>(Func<T> operation, T defaultValue, string operationName, string key = null)
        {
            if (_resiliencePolicy != null)
            {
                try
                {
                    var context = new Context(operationName);
                    if (!string.IsNullOrEmpty(key))
                    {
                        context["Key"] = key;
                    }

                    // Convert sync operation to async for Polly
                    return _resiliencePolicy.ExecuteAsync(async (ctx) => await Task.Run(operation), context).GetAwaiter().GetResult();
                }
                catch (Exception ex) when (IsConnectionFailure(ex))
                {
                    _logger.Log(_options.ConnectionFailureLogLevel, ex,
                        "Database connection failed for {Operation} operation. Key: {Key}", operationName, key);
                    return defaultValue;
                }
            }
            else
            {
                // When resilience patterns are disabled, execute operation directly without catching exceptions
                return operation();
            }
        }

        /// <summary>
        /// Executes a void async operation with resilience policies if enabled.
        /// </summary>
        private async Task ExecuteWithResilienceAsync(Func<Task> operation, string operationName, string key = null)
        {
            if (_resiliencePolicy != null)
            {
                try
                {
                    var context = new Context(operationName);
                    if (!string.IsNullOrEmpty(key))
                    {
                        context["Key"] = key;
                    }

                    await _resiliencePolicy.ExecuteAsync(async (ctx) => await operation(), context);
                }
                catch (Exception ex) when (IsConnectionFailure(ex))
                {
                    _logger.Log(_options.ConnectionFailureLogLevel, ex,
                        "Database connection failed for {Operation} operation. Key: {Key}", operationName, key);
                    // Silently fail for void operations
                }
            }
            else
            {
                // When resilience patterns are disabled, execute operation directly without catching exceptions
                await operation();
            }
        }

        /// <summary>
        /// Executes a void sync operation with resilience policies if enabled.
        /// </summary>
        private void ExecuteWithResilience(Action operation, string operationName, string key = null)
        {
            if (_resiliencePolicy != null)
            {
                try
                {
                    var context = new Context(operationName);
                    if (!string.IsNullOrEmpty(key))
                    {
                        context["Key"] = key;
                    }

                    // Convert sync operation to async for Polly
                    _resiliencePolicy.ExecuteAsync(async (ctx) => await Task.Run(operation), context).GetAwaiter().GetResult();
                }
                catch (Exception ex) when (IsConnectionFailure(ex))
                {
                    _logger.Log(_options.ConnectionFailureLogLevel, ex,
                        "Database connection failed for {Operation} operation. Key: {Key}", operationName, key);
                    // Silently fail for void operations
                }
            }
            else
            {
                // When resilience patterns are disabled, execute operation directly without catching exceptions
                operation();
            }
        }

        /// <summary>
        /// Determines if an exception is a connection failure that should be handled gracefully.
        /// </summary>
        private static bool IsConnectionFailure(Exception ex)
        {
            return ex is PostgresException pgEx &&
                   (IsTransientException(pgEx) || IsPermanentException(pgEx)) ||
                   ex is NpgsqlException npgsqlEx &&
                   (npgsqlEx.Message.Contains("reading from stream") ||
                    npgsqlEx.Message.Contains("connection") ||
                    npgsqlEx.InnerException is System.IO.IOException ||
                    npgsqlEx.InnerException is System.Net.Sockets.SocketException) ||
                   ex is TimeoutException ||
                   ex is Polly.Timeout.TimeoutRejectedException ||
                   ex is Polly.CircuitBreaker.BrokenCircuitException ||
                   ex is System.Net.Sockets.SocketException ||
                   ex is System.IO.IOException && ex.Message.Contains("transport connection") ||
                   ex is InvalidOperationException && ex.Message.Contains("connection") ||
                   ex is ObjectDisposedException && ex.Message.Contains("connection");
        }

        /// <summary>
        /// Determines if a PostgreSQL exception is transient and should be retried.
        /// </summary>
        private static bool IsTransientException(PostgresException ex)
        {
            return ex.SqlState switch
            {
                // Connection failures (likely temporary)
                "08001" => true, // Connection failed - server unavailable or network issue
                "08006" => true, // Connection failure - connection lost during operation
                "08000" => true, // Connection exception - general connection problem
                "08003" => true, // Connection does not exist - connection was closed
                "08004" => true, // SQL server rejected establishment of SQL connection - server overload
                "08007" => true, // Connection failure during transaction - network interruption

                // Resource exhaustion (likely temporary)
                "53300" => true, // Too many connections - connection pool exhausted
                "57014" => true, // Query canceled - server canceled due to resource constraints
                "57000" => true, // Statement timeout - query took too long, server busy

                // Server issues (likely temporary)
                "57P01" => true, // Admin shutdown - server shutting down for maintenance
                "57P02" => true, // Crash shutdown - server crashed, will restart
                "57P03" => true, // Cannot connect now - server temporarily unavailable
                "57P04" => true, // Database shutdown - database shutting down
                "57P05" => true, // Database restart - database restarting

                // Network issues (likely temporary)
                "XX000" => true, // Internal error - some internal errors are transient

                _ => false
            };
        }

        /// <summary>
        /// Determines if a PostgreSQL exception is permanent and should not be retried.
        /// </summary>
        private static bool IsPermanentException(PostgresException ex)
        {
            return ex.SqlState switch
            {
                // Authentication failures
                "28P01" => true, // Password authentication failed
                "28P02" => true, // Password authentication failed
                "28P03" => true, // Password authentication failed
                "28P04" => true, // Password authentication failed

                // Authorization failures
                "42501" => true, // Insufficient privilege
                "42502" => true, // Insufficient privilege
                "42503" => true, // Insufficient privilege
                "42504" => true, // Insufficient privilege
                "42505" => true, // Insufficient privilege
                "42506" => true, // Insufficient privilege

                // Configuration errors
                "3D000" => true, // Invalid catalog name
                "3F000" => true, // Invalid schema name
                "42P01" => true, // Undefined table
                "42P02" => true, // Undefined parameter
                "42P03" => true, // Undefined column
                "42P04" => true, // Undefined object

                // Data type errors
                "22P02" => true, // Invalid text representation
                "22P03" => true, // Invalid binary representation
                "22P04" => true, // Bad copy file format
                "22P05" => true, // Untranslatable character

                _ => false
            };
        }

        public void Dispose()
        {
            _connectionStringProvider?.Dispose();
        }
    }
}