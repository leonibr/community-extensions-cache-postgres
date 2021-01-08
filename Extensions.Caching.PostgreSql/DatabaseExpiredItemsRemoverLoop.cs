using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Internal;

namespace Community.Microsoft.Extensions.Caching.PostgreSql
{
    internal class DatabaseExpiredItemsRemoverLoop
    {
        private readonly ISystemClock _systemClock;
        private readonly TimeSpan _expiredItemsDeletionInterval;
        private DateTimeOffset _lastExpirationScan;
        private readonly IDatabaseOperations _databaseOperations;
        private readonly CancellationTokenSource _cancellationTokenSource;

        internal DatabaseExpiredItemsRemoverLoop(
            ISystemClock systemClock,
            IDatabaseOperations databaseOperations,
            TimeSpan expiredItemsDeletionInterval,
            IHostApplicationLifetime applicationLifetime)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            applicationLifetime.ApplicationStopping.Register(OnShutdown);
            _databaseOperations = databaseOperations;
            _systemClock = systemClock;
            _expiredItemsDeletionInterval = expiredItemsDeletionInterval;
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
