using Microsoft.Extensions.Caching.Distributed;
using System;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using Microsoft.Extensions.Internal;
using System.Threading;
using Microsoft.Extensions.Hosting;


namespace Community.Microsoft.Extensions.Caching.PostgreSql
{
	public class PostgreSqlCache : IDistributedCache
	{
		private static readonly TimeSpan MinimumExpiredItemsDeletionInterval = TimeSpan.FromMinutes(5);
		private static readonly TimeSpan DefaultExpiredItemsDeletionInterval = TimeSpan.FromMinutes(30);

		private readonly IDatabaseOperations _dbOperations;
		private readonly ISystemClock _systemClock;
		private readonly TimeSpan _defaultSlidingExpiration;
		private readonly DatabaseExpiredItemsRemoverLoop _deleteLoop;

		public PostgreSqlCache(IOptions<PostgreSqlCacheOptions> options, IHostApplicationLifetime applicationLifetime)
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
			if (cacheOptions.ExpiredItemsDeletionInterval.HasValue &&
				cacheOptions.ExpiredItemsDeletionInterval.Value < MinimumExpiredItemsDeletionInterval)
			{
				throw new ArgumentException(
					$"{nameof(PostgreSqlCacheOptions.ExpiredItemsDeletionInterval)} cannot be less the minimum " +
					$"value of {MinimumExpiredItemsDeletionInterval.TotalMinutes} minutes.");
			}
			if (cacheOptions.DefaultSlidingExpiration <= TimeSpan.Zero)
			{
				throw new ArgumentOutOfRangeException(
					nameof(cacheOptions.DefaultSlidingExpiration),
					cacheOptions.DefaultSlidingExpiration,
					"The sliding expiration value must be positive.");
			}

			_systemClock = cacheOptions.SystemClock ?? new SystemClock();
			var expiredItemsDeletionInterval =
				cacheOptions.ExpiredItemsDeletionInterval ?? DefaultExpiredItemsDeletionInterval;
			_defaultSlidingExpiration = cacheOptions.DefaultSlidingExpiration;

			_dbOperations = new DatabaseOperations(
				   cacheOptions.ConnectionString,
				   cacheOptions.SchemaName,
				   cacheOptions.TableName,
				   cacheOptions.CreateInfrastructure,
				   _systemClock);

			_deleteLoop = new DatabaseExpiredItemsRemoverLoop(_systemClock, _dbOperations, expiredItemsDeletionInterval, applicationLifetime);
		}


		public byte[] Get(string key)
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}

			return _dbOperations.GetCacheItem(key);
		}

		public Task<byte[]> GetAsync(string key, CancellationToken token = default(CancellationToken))
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}

			return _dbOperations.GetCacheItemAsync(key);
		}

		public void Refresh(string key)
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}

			_dbOperations.RefreshCacheItem(key);
		}

		public async Task RefreshAsync(string key, CancellationToken token = default(CancellationToken))
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}

			await _dbOperations.RefreshCacheItemAsync(key);
		}

		public void Remove(string key)
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}

			_dbOperations.DeleteCacheItem(key);
		}

		public Task RemoveAsync(string key, CancellationToken token = default(CancellationToken))
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}

			return _dbOperations.DeleteCacheItemAsync(key);
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

		public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default(CancellationToken))
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

			return _dbOperations.SetCacheItemAsync(key, value, options);
		}

		private void DeleteExpiredCacheItems()
		{
			_dbOperations.DeleteExpiredCacheItems();
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
