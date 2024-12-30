using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Community.Microsoft.Extensions.Caching.PostgreSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PostgreSqlCacheSample
{
	public class Program
	{
		public static void Main(string[] args)
		{
			CreateHostBuilder(args).Build().Run();
		}
		
		private static IHostBuilder CreateHostBuilder(string[] args) =>
			Host.CreateDefaultBuilder(args)
				.ConfigureLogging(logging=>
				{
					logging.ClearProviders();
					logging.AddConsole();
				})
				.ConfigureServices((hostContext, services) =>
				{
					var configuration = hostContext.Configuration;
					services.AddDistributedPostgreSqlCache(setup =>
					{
						setup.ConnectionString = configuration["ConnectionString"];
						setup.SchemaName = configuration["SchemaName"];
						setup.TableName = configuration["TableName"];
						setup.CreateInfrastructure = configuration.GetValue<bool>("CreateInfrastructure");
						// CreateInfrastructure is optional, default is TRUE
						// This means que every time starts the application the 
						// creation of table and database functions will be verified.
					})
						
					// if you need to resolve something from the serviceprovider for configuration
					// use this overload
					// services.AddDistributedPostgreSqlCache((serviceProvider, setup) =>
					// {
					//	// IConfiguration is used as an example here
					// 	var configuration = serviceProvider.GetRequiredService<IConfiguration>();
					// 	setup.ConnectionString = configuration["ConnectionString"];
					// 	setup.SchemaName = configuration["SchemaName"];
					// 	setup.TableName = configuration["TableName"];
					// 	setup.CreateInfrastructure = configuration.GetValue<bool>("CreateInfrastructure");
					// })
                    .AddHostedService<Worker>();
				});
	}
}
