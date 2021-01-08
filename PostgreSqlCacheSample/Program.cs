using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Community.Microsoft.Extensions.Caching.PostgreSql;

namespace PostgreSqlCacheSample
{
	public class Program
	{
		public static void Main(string[] args)
		{
			CreateHostBuilder(args).Build().Run();
		}

		public static IHostBuilder CreateHostBuilder(string[] args) =>
			Host.CreateDefaultBuilder(args)
				.ConfigureServices((hostContext, services) =>
				{
					var configuration = hostContext.Configuration;
					services.AddDistributedPostgreSqlCache(setup =>
					{
						setup.ConnectionString = configuration["ConnectionString"];
						setup.SchemaName = configuration["SchemaName"];
						setup.TableName = configuration["TableName"];
						setup.CreateInfrastructure = !string.IsNullOrWhiteSpace(configuration["CreateInfrastructure"]);
						// CreateInfrastructure is optional, default is TRUE
						// This means que every time starts the application the 
						// creation of table and database functions will be verified.
					});
					services.AddHostedService<Worker>();
				});
	}
}
