using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;
using System;

namespace Community.Microsoft.Extensions.Caching.PostgreSql
{
	public class PostgreSqlCacheOptions : IOptions<PostgreSqlCacheOptions>
    {
        /// <summary>
        /// An abstraction to represent the clock of a machine in order to enable unit testing.
        /// </summary>
        public ISystemClock SystemClock { get; set; } = new SystemClock();

        /// <summary>
        /// The periodic interval to scan and delete expired items in the cache. Default is 30 minutes.
        /// </summary>
        public TimeSpan? ExpiredItemsDeletionInterval { get; set; }

        /// <summary>
        /// The connection string to the database.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// The schema name of the table.
        /// </summary>
        public string SchemaName { get; set; }

        /// <summary>
        /// Name of the table where the cache items are stored.
        /// </summary>
        public string TableName { get; set; }

		/// <summary>
		/// If set to true will create table and functions if necessary every time an instance of PostgreSqlCache is created
		/// </summary>
		public bool CreateInfrastructure { get; set; } = true;

		/// <summary>
		/// The default sliding expiration set for a cache entry if neither Absolute or SlidingExpiration has been set explicitly.
		/// By default, its 20 minutes.
		/// </summary>
		public TimeSpan DefaultSlidingExpiration { get; set; } = TimeSpan.FromMinutes(20);

        PostgreSqlCacheOptions IOptions<PostgreSqlCacheOptions>.Value => this;
    }
}
