using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;
using System;
using Npgsql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Community.Microsoft.Extensions.Caching.PostgreSql
{
    public class PostgreSqlCacheOptions : IOptions<PostgreSqlCacheOptions>
    {
        /// <summary>
        /// The factory to create a NpgsqlDataSource instance.
        /// Either <see cref="DataSourceFactory"/> or <see cref="ConnectionString"/> should be set.
        /// </summary>
        public Func<NpgsqlDataSource> DataSourceFactory { get; set; }

        /// <summary>
        /// The connection string to the database.
        /// If <see cref="DataSourceFactory"/> not set, <see cref="ConnectionString"/> would be used to connect to the database.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Configuration key for the connection string. Used for reloading from configuration sources like Azure Key Vault.
        /// If set, the connection string will be reloaded from configuration when needed.
        /// </summary>
        public string ConnectionStringKey { get; set; }

        /// <summary>
        /// Configuration instance for reloading connection strings. Required when using <see cref="ConnectionStringKey"/>.
        /// </summary>
        public IConfiguration Configuration { get; set; }

        /// <summary>
        /// Logger instance for connection string reloading operations.
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Time interval to check for connection string updates. Default is 5 minutes.
        /// Only used when <see cref="ConnectionStringKey"/> is set.
        /// </summary>
        public TimeSpan ConnectionStringReloadInterval { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Whether to enable automatic connection string reloading from configuration.
        /// Default is false.
        /// </summary>
        public bool EnableConnectionStringReloading { get; set; } = false;

        /// <summary>
        /// An abstraction to represent the clock of a machine in order to enable unit testing.
        /// </summary>
        public ISystemClock SystemClock { get; set; } = new SystemClock();

        /// <summary>
        /// The periodic interval to scan and delete expired items in the cache. Default is 30 minutes. 
        /// Minimum allowed is 5 minutes.
        /// </summary>
        public TimeSpan? ExpiredItemsDeletionInterval { get; set; }

        /// <summary>
        /// The schema name of the table.
        /// </summary>
        public string SchemaName { get; set; }

        /// <summary>
        /// Name of the table where the cache items are stored.
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// If set to true will create table and functions if necessary every time an instance of PostgreSqlCache is created.
        /// </summary>
        public bool CreateInfrastructure { get; set; } = true;

        /// <summary>
        /// The default sliding expiration set for a cache entry if neither Absolute or SlidingExpiration has been set explicitly.
        /// By default, its 20 minutes.
        /// </summary>
        public TimeSpan DefaultSlidingExpiration { get; set; } = TimeSpan.FromMinutes(20);

        /// <summary>
        /// If set to true this instance of the cache will not remove expired items. 
        /// Maybe on multiple instances scenario sharing the same database another instance will be responsible for remove expired items.
        /// Default value is false.
        /// </summary>
        public bool DisableRemoveExpired { get; set; } = false;

        /// <summary>
        /// If set to false no update of ExpiresAtTime will be performed when getting a cache item (i.e., IDistributedCache.Get or GetAsync)
        /// Default value is true. 
        /// ATTENTION: When is set to false the user of the distributed cache must call the IDistributedCache.Refresh to update slide expiration.
        ///   For example, if you are using ASPNET Core Sessions, ASPNET Core will call IDistributedCache.Refresh for you at the end of the request if 
        ///   needed (i.e., there wasn't any changes to the session but it still needs to be refreshed).
        /// </summary>
        public bool UpdateOnGetCacheItem { get; set; } = true;

        /// <summary>
        /// If set to true, no updates at all will be saved to the database, values will only be read.
        /// ATTENTION: this will disable any sliding expiration as well as cache clean-up.
        /// </summary>
        public bool ReadOnlyMode { get; set; } = false;

        /// <summary>
        /// Enables Polly-based resilience patterns for database operations.
        /// Default is true.
        /// </summary>
        public bool EnableResiliencePatterns { get; set; } = true;

        /// <summary>
        /// Maximum number of retry attempts for transient failures.
        /// Default is 3.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Base delay for retry attempts. Uses exponential backoff.
        /// Default is 1 second.
        /// </summary>
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Enables circuit breaker pattern to prevent cascading failures.
        /// Default is true.
        /// </summary>
        public bool EnableCircuitBreaker { get; set; } = true;

        /// <summary>
        /// Number of consecutive failures before opening circuit breaker.
        /// Default is 5.
        /// </summary>
        public int CircuitBreakerFailureThreshold { get; set; } = 5;

        /// <summary>
        /// Duration to keep circuit breaker open before attempting reset.
        /// Default is 1 minute.
        /// </summary>
        public TimeSpan CircuitBreakerDurationOfBreak { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Log level for connection failure messages.
        /// Default is Warning.
        /// </summary>
        public LogLevel ConnectionFailureLogLevel { get; set; } = LogLevel.Warning;

        /// <summary>
        /// Timeout for database operations.
        /// Default is 30 seconds.
        /// </summary>
        public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Enables detailed logging of resilience pattern execution.
        /// Default is false.
        /// </summary>
        public bool EnableResilienceLogging { get; set; } = false;

        /// <summary>
        /// Validates the resilience configuration options.
        /// </summary>
        public void ValidateResilienceConfiguration()
        {
            if (!EnableResiliencePatterns)
            {
                // Log warning if sub-options are configured but master switch is off
                if (Logger != null && (EnableCircuitBreaker || MaxRetryAttempts != 3 || !RetryDelay.Equals(TimeSpan.FromSeconds(1)) ||
                    CircuitBreakerFailureThreshold != 5 || !CircuitBreakerDurationOfBreak.Equals(TimeSpan.FromMinutes(1)) ||
                    !OperationTimeout.Equals(TimeSpan.FromSeconds(30))))
                {
                    var scope = Logger.BeginScope("Resilience patterns are disabled but one or more sub-options are configured. This will have no effect.");

                    Logger.LogWarning("EnableResiliencePatterns: {EnableResiliencePatterns}", EnableResiliencePatterns);
                    if (EnableCircuitBreaker)
                    {
                        Logger.LogWarning("Not effective: EnableCircuitBreaker: {EnableCircuitBreaker}", EnableCircuitBreaker);
                    }
                    if (MaxRetryAttempts != 3)
                    {
                        Logger.LogWarning("Not effective: MaxRetryAttempts: {MaxRetryAttempts}", MaxRetryAttempts);
                    }
                    if (!RetryDelay.Equals(TimeSpan.FromSeconds(1)))
                    {
                        Logger.LogWarning("Not effective: RetryDelay: {RetryDelay}", RetryDelay);
                    }
                    if (CircuitBreakerFailureThreshold != 5)
                    {
                        Logger.LogWarning("Not effective: CircuitBreakerFailureThreshold: {CircuitBreakerFailureThreshold}", CircuitBreakerFailureThreshold);
                    }
                    if (!CircuitBreakerDurationOfBreak.Equals(TimeSpan.FromMinutes(1)))
                    {
                        Logger.LogWarning("Not effective: CircuitBreakerDurationOfBreak: {CircuitBreakerDurationOfBreak}", CircuitBreakerDurationOfBreak);
                    }
                    if (!OperationTimeout.Equals(TimeSpan.FromSeconds(30)))
                    {
                        Logger.LogWarning("Not effective: OperationTimeout: {OperationTimeout}", OperationTimeout);
                    }
                    scope.Dispose();
                }
                return;
            }

            if (MaxRetryAttempts < 0)
                throw new ArgumentException($"{nameof(MaxRetryAttempts)} must be non-negative.");

            if (MaxRetryAttempts > 10)
                throw new ArgumentException($"{nameof(MaxRetryAttempts)} cannot exceed 10.");

            if (RetryDelay <= TimeSpan.Zero)
                throw new ArgumentException($"{nameof(RetryDelay)} must be positive.");

            if (RetryDelay > TimeSpan.FromMinutes(5))
                throw new ArgumentException($"{nameof(RetryDelay)} cannot exceed 5 minutes.");

            if (CircuitBreakerFailureThreshold < 1)
                throw new ArgumentException($"{nameof(CircuitBreakerFailureThreshold)} must be at least 1.");

            if (CircuitBreakerFailureThreshold > 100)
                throw new ArgumentException($"{nameof(CircuitBreakerFailureThreshold)} cannot exceed 100.");

            if (CircuitBreakerDurationOfBreak <= TimeSpan.Zero)
                throw new ArgumentException($"{nameof(CircuitBreakerDurationOfBreak)} must be positive.");

            if (CircuitBreakerDurationOfBreak > TimeSpan.FromHours(1))
                throw new ArgumentException($"{nameof(CircuitBreakerDurationOfBreak)} cannot exceed 1 hour.");

            if (OperationTimeout <= TimeSpan.Zero)
                throw new ArgumentException($"{nameof(OperationTimeout)} must be positive.");

            if (OperationTimeout > TimeSpan.FromMinutes(10))
                throw new ArgumentException($"{nameof(OperationTimeout)} cannot exceed 10 minutes.");
        }

        PostgreSqlCacheOptions IOptions<PostgreSqlCacheOptions>.Value => this;
    }
}
