using Microsoft.Extensions.Caching.Distributed;
using System;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using System.Threading;

namespace Community.Microsoft.Extensions.Caching.PostgreSql
{
    internal sealed class PostgreSqlCache : IDistributedCache
    {
        private readonly IDatabaseOperations _dbOperations;
        private readonly TimeSpan _defaultSlidingExpiration;

        public PostgreSqlCache(
            IOptions<PostgreSqlCacheOptions> options,
            IDatabaseOperations databaseOperations,
            IDatabaseExpiredItemsRemoverLoop loop)
        {
            _dbOperations = databaseOperations ?? throw new ArgumentNullException(nameof(databaseOperations));

            _ = loop ?? throw new ArgumentNullException(nameof(loop));

            var cacheOptions = options?.Value ?? throw new ArgumentNullException(nameof(options));

            if (cacheOptions.DefaultSlidingExpiration <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cacheOptions.DefaultSlidingExpiration),
                    cacheOptions.DefaultSlidingExpiration,
                    "The sliding expiration value must be positive.");
            }

            _defaultSlidingExpiration = cacheOptions.DefaultSlidingExpiration;

            loop.Start();
        }

        public byte[] Get(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return _dbOperations.GetCacheItem(key);
        }

        public Task<byte[]> GetAsync(string key, CancellationToken token)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return _dbOperations.GetCacheItemAsync(key, token);
        }

        public void Refresh(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            _dbOperations.RefreshCacheItem(key);
        }

        public async Task RefreshAsync(string key, CancellationToken token)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            await _dbOperations.RefreshCacheItemAsync(key, token);
        }

        public void Remove(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            _dbOperations.DeleteCacheItem(key);
        }

        public Task RemoveAsync(string key, CancellationToken token)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return _dbOperations.DeleteCacheItemAsync(key, token);
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            GetOptions(ref options);

            _dbOperations.SetCacheItem(key, value, options);
        }

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            GetOptions(ref options);

            return _dbOperations.SetCacheItemAsync(key, value, options, token);
        }

        private void GetOptions(ref DistributedCacheEntryOptions options)
        {
            if (!options.AbsoluteExpiration.HasValue
                && !options.AbsoluteExpirationRelativeToNow.HasValue
                && !options.SlidingExpiration.HasValue)
            {
                options = new DistributedCacheEntryOptions()
                {
                    SlidingExpiration = _defaultSlidingExpiration
                };
            }
        }
    }
}
