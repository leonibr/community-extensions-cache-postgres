using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;
using System;
using Npgsql;

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

        PostgreSqlCacheOptions IOptions<PostgreSqlCacheOptions>.Value => this;
    }
}
