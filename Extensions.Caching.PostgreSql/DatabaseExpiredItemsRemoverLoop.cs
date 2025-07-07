using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Community.Microsoft.Extensions.Caching.PostgreSql
{
    internal sealed class DatabaseExpiredItemsRemoverLoop : IDatabaseExpiredItemsRemoverLoop
    {
        private static readonly TimeSpan MinimumExpiredItemsDeletionInterval = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan DefaultExpiredItemsDeletionInterval = TimeSpan.FromMinutes(30);
        private readonly TimeSpan _expiredItemsDeletionInterval;
        private DateTimeOffset _lastExpirationScan;
        private bool disposedValue;
        private readonly ILogger<DatabaseExpiredItemsRemoverLoop> _logger;
        private readonly IDatabaseOperations _databaseOperations;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ISystemClock _systemClock;
        private readonly bool _disabled;

        public DatabaseExpiredItemsRemoverLoop(
            IOptions<PostgreSqlCacheOptions> options,
            IDatabaseOperations databaseOperations,
            ILogger<DatabaseExpiredItemsRemoverLoop> logger) : this(options, databaseOperations, null, logger)
        {
            logger.LogDebug("IHostApplicationLifetime NOT FOUND ON THIS PLATFORM");

        }

        public DatabaseExpiredItemsRemoverLoop(
            IOptions<PostgreSqlCacheOptions> options,
            IDatabaseOperations databaseOperations,
            IHostApplicationLifetime applicationLifetime,
            ILogger<DatabaseExpiredItemsRemoverLoop> logger)
        {
            this._logger = logger;
            var cacheOptions = options.Value;

            if ((_disabled = cacheOptions.DisableRemoveExpired) == true)
            {
                //No need to configure anything
                return;
            }

            if (cacheOptions.ExpiredItemsDeletionInterval.HasValue &&
                cacheOptions.ExpiredItemsDeletionInterval.Value < MinimumExpiredItemsDeletionInterval)
            {
                throw new ArgumentException(
                    $"{nameof(PostgreSqlCacheOptions.ExpiredItemsDeletionInterval)} cannot be less the minimum " +
                    $"value of {MinimumExpiredItemsDeletionInterval.TotalMinutes} minutes.");
            }

            _systemClock = cacheOptions.SystemClock;
            _cancellationTokenSource = new CancellationTokenSource();
            if (applicationLifetime != null)
            {
                applicationLifetime.ApplicationStopping.Register(OnShutdown);
            }
            this._databaseOperations = databaseOperations;
            _expiredItemsDeletionInterval = cacheOptions.ExpiredItemsDeletionInterval ?? DefaultExpiredItemsDeletionInterval;
        }

        public void Start()
        {
            if (_disabled)
            {
                return;
            }

            Task.Run(DeleteExpiredCacheItems);
        }

        private void OnShutdown()
        {
            _cancellationTokenSource.Cancel();
        }

        private async Task DeleteExpiredCacheItems()
        {
            while (true)
            {
                var utcNow = _systemClock.UtcNow;
                if ((utcNow - _lastExpirationScan) > _expiredItemsDeletionInterval)
                {
                    try
                    {
                        _logger.LogDebug($"DeleteExpiredCacheItems executing");
                        await _databaseOperations.DeleteExpiredCacheItemsAsync(_cancellationTokenSource.Token);
                        _lastExpirationScan = utcNow;
                        _logger.LogDebug($"DeleteExpiredCacheItems executed at {utcNow}");
                    }
                    catch (TaskCanceledException)
                    {
                        _logger.LogDebug($"DeleteExpiredCacheItems was cancelled at {utcNow}");
                        break;
                    }
                    catch (Exception ex)
                    {
                        //We don't want transient errors from failing next run
                        _logger.LogError(ex, "An error occurred while deleting expired cache items.");
                    }
                }

                try
                {
                    _logger.LogDebug($"Task Delay interval will sleep for {_expiredItemsDeletionInterval}s");
                    await Task
                            .Delay(_expiredItemsDeletionInterval, _cancellationTokenSource.Token)
                            .ConfigureAwait(true);
                    _logger.LogDebug($"Task Delay interval resumed after {_expiredItemsDeletionInterval}s");
                }
                catch (TaskCanceledException)
                {
                    // ignore: Task.Delay throws this exception when ct.IsCancellationRequested = true
                    // In this case, we only want to stop polling and finish this async Task.
                    _logger.LogDebug("Task Delay interval Cancelled.");
                    break;
                }
            }
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _cancellationTokenSource?.Cancel();
                    _cancellationTokenSource?.Dispose();
                }
                disposedValue = true;
            }
        }



        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
