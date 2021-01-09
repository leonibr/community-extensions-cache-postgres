// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Distributed;

namespace Community.Microsoft.Extensions.Caching.PostgreSql
{
	/// <summary>
	/// Extension methods for setting up PostgreSql distributed cache services in an <see cref="IServiceCollection" />.
	/// </summary>
	public static class PostGreSqlCachingServicesExtensions
	{
		/// <summary>
		/// Adds Community Microsoft PostgreSql distributed caching services to the specified <see cref="IServiceCollection" />.
		/// </summary>
		/// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
		/// <param name="setupAction">An <see cref="Action{PostgreSqlCacheOptions}"/> to configure the provided <see cref="PostgreSqlCacheOptions"/>.</param>
		/// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
		public static IServiceCollection AddDistributedPostgreSqlCache(this IServiceCollection services, Action<PostgreSqlCacheOptions> setupAction)
		{
			if (services == null)
			{
				throw new ArgumentNullException(nameof(services));
			}

			if (setupAction == null)
			{
				throw new ArgumentNullException(nameof(setupAction));
			}

			services.AddOptions();
			AddPostgreSqlCacheServices(services);
			services.Configure(setupAction);

			return services;
		}

		// to enable unit testing
        private static void AddPostgreSqlCacheServices(IServiceCollection services)
		{
            services.AddSingleton<IDatabaseOperations, DatabaseOperations>();
			services.AddSingleton<IDatabaseExpiredItemsRemoverLoop, DatabaseExpiredItemsRemoverLoop>();
			services.AddSingleton<IDistributedCache, PostgreSqlCache>();
		}
	}
}