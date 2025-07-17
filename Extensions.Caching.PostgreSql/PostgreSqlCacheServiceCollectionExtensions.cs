// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Community.Microsoft.Extensions.Caching.PostgreSql
{
	/// <summary>
	/// Extension methods for setting up PostgreSql distributed cache services in an <see cref="IServiceCollection" />.
	/// </summary>
	public static class PostGreSqlCachingServicesExtensions
	{
		/// <summary>
		/// Adds Community Microsoft PostgreSql distributed caching services to the specified <see cref="IServiceCollection" />
		/// without configuration. Use an implementation of <see cref="IConfigureOptions{PostgreSqlCacheOptions}"/> for configuration.
		/// </summary>
		/// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
		/// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
		public static IServiceCollection AddDistributedPostgreSqlCache(this IServiceCollection services)
		{
			if (services == null)
			{
				throw new ArgumentNullException(nameof(services));
			}

			services.AddOptions();
			AddPostgreSqlCacheServices(services);

			return services;
		}

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

		/// <summary>
		/// Adds Community Microsoft PostgreSql distributed caching services to the specified <see cref="IServiceCollection" />.
		/// </summary>
		/// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
		/// <param name="setupAction">An <see cref="Action{IServiceProvider, PostgreSqlCacheOptions}"/> to configure the provided <see cref="PostgreSqlCacheOptions"/>.</param>
		/// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
		public static IServiceCollection AddDistributedPostgreSqlCache(this IServiceCollection services, Action<IServiceProvider, PostgreSqlCacheOptions> setupAction)
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
			services.AddSingleton<IConfigureOptions<PostgreSqlCacheOptions>>(
				sp => new ConfigureOptions<PostgreSqlCacheOptions>(opt => setupAction(sp, opt)));

			return services;
		}

		/// <summary>
		/// Adds PostgreSQL distributed caching services with reloadable connection string support for Azure Key Vault rotation.
		/// </summary>
		/// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
		/// <param name="connectionStringKey">The configuration key for the connection string.</param>
		/// <param name="setupAction">An <see cref="Action{PostgreSqlCacheOptions}" /> to configure additional options.</param>
		/// <returns>The <see cref="IServiceCollection" /> so that additional calls can be chained.</returns>
		public static IServiceCollection AddDistributedPostgreSqlCacheWithReloadableConnection(
			this IServiceCollection services,
			string connectionStringKey,
			Action<PostgreSqlCacheOptions> setupAction = null)
		{
			if (services == null)
			{
				throw new ArgumentNullException(nameof(services));
			}

			if (string.IsNullOrEmpty(connectionStringKey))
			{
				throw new ArgumentException("Connection string key cannot be null or empty.", nameof(connectionStringKey));
			}

			services.AddOptions();
			AddPostgreSqlCacheServices(services);
			services.AddSingleton<IConfigureOptions<PostgreSqlCacheOptions>>(
				sp => new ConfigureOptions<PostgreSqlCacheOptions>(options =>
				{
					var configuration = sp.GetRequiredService<IConfiguration>();
					var logger = sp.GetRequiredService<ILogger<PostgreSqlCache>>();

					options.ConnectionStringKey = connectionStringKey;
					options.Configuration = configuration;
					options.Logger = logger;
					options.EnableConnectionStringReloading = true;

					setupAction?.Invoke(options);
				}));

			return services;
		}

		/// <summary>
		/// Adds PostgreSQL distributed caching services with reloadable connection string support for Azure Key Vault rotation.
		/// </summary>
		/// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
		/// <param name="connectionStringKey">The configuration key for the connection string.</param>
		/// <param name="reloadInterval">The interval to check for connection string updates.</param>
		/// <param name="setupAction">An <see cref="Action{PostgreSqlCacheOptions}" /> to configure additional options.</param>
		/// <returns>The <see cref="IServiceCollection" /> so that additional calls can be chained.</returns>
		public static IServiceCollection AddDistributedPostgreSqlCacheWithReloadableConnection(
			this IServiceCollection services,
			string connectionStringKey,
			TimeSpan reloadInterval,
			Action<PostgreSqlCacheOptions> setupAction = null)
		{
			if (services == null)
			{
				throw new ArgumentNullException(nameof(services));
			}

			if (string.IsNullOrEmpty(connectionStringKey))
			{
				throw new ArgumentException("Connection string key cannot be null or empty.", nameof(connectionStringKey));
			}

			services.AddOptions();
			AddPostgreSqlCacheServices(services);
			services.AddSingleton<IConfigureOptions<PostgreSqlCacheOptions>>(
				sp => new ConfigureOptions<PostgreSqlCacheOptions>(options =>
				{
					var configuration = sp.GetRequiredService<IConfiguration>();
					var logger = sp.GetRequiredService<ILogger<PostgreSqlCache>>();

					options.ConnectionStringKey = connectionStringKey;
					options.Configuration = configuration;
					options.Logger = logger;
					options.EnableConnectionStringReloading = true;
					options.ConnectionStringReloadInterval = reloadInterval;

					setupAction?.Invoke(options);
				}));

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