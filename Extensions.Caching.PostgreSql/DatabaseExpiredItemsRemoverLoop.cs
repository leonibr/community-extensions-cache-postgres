using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;

namespace Community.Microsoft.Extensions.Caching.PostgreSql
{
    internal sealed class DatabaseExpiredItemsRemoverLoop : IDatabaseExpiredItemsRemoverLoop
    {
        private static readonly TimeSpan MinimumExpiredItemsDeletionInterval = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan DefaultExpiredItemsDeletionInterval = TimeSpan.FromMinutes(30);
        private readonly TimeSpan _expiredItemsDeletionInterval;
        private DateTimeOffset _lastExpirationScan;
        private readonly IDatabaseOperations _databaseOperations;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ISystemClock _systemClock;
        private readonly bool _disabled;

        public DatabaseExpiredItemsRemoverLoop(
            IOptions<PostgreSqlCacheOptions> options,
            IDatabaseOperations databaseOperations,
            IHostApplicationLifetime applicationLifetime)
        {
            var cacheOptions = options.Value;

            if ((_disabled = cacheOptions.Disabled) == true)
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
            applicationLifetime.ApplicationStopping.Register(OnShutdown);
            _databaseOperations = databaseOperations;
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
                        await _databaseOperations.DeleteExpiredCacheItemsAsync(_cancellationTokenSource.Token);
                        _lastExpirationScan = utcNow;
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                    catch (Exception)
                    {
                        //We don't want transient errors from failing next run
                    }
                }

                try
                {
                    await Task
                            .Delay(_expiredItemsDeletionInterval, _cancellationTokenSource.Token)
                            .ConfigureAwait(true);
                }
                catch (TaskCanceledException)
                {
                    // ignore: Task.Delay throws this exception when ct.IsCancellationRequested = true
                    // In this case, we only want to stop polling and finish this async Task.
                    break;
                }
            }
        }
    }
}
