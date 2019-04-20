// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Community.Microsoft.Extensions.Caching.PostgreSql;
using Microsoft.Extensions.Caching.Distributed;
using System.Threading;
using Nito.AsyncEx;

namespace PostgreSqlCacheSample
{
    /// <summary>
    /// This sample requires setting up a PostgreSQL database called 'cache_test'.
    /// Execute the command 'prepare-database.cmd -create' at the root of the project
    /// Then run this sample by doing "dotnet run"
    /// </summary>
    public class Program
    {
        public static void Main()
        {
            AsyncContext.Run(() => RunSampleAsync());
        }

        public static async Task RunSampleAsync()
        {
            var configurationBuilder = new ConfigurationBuilder();
            var configuration = configurationBuilder
                .AddJsonFile("config.json")
                .AddEnvironmentVariables()
                .Build();

            var key = Guid.NewGuid().ToString();
            var message = "Hello, World!";
            var value = Encoding.UTF8.GetBytes(message);

            Console.WriteLine("Connecting to cache");
            var cache = new PostgreSqlCache(new PostgreSqlCacheOptions()
            {
                ConnectionString = configuration["ConnectionString"],
                SchemaName = configuration["SchemaName"],
                TableName = configuration["TableName"],
            });

            Console.WriteLine("Connected");

            Console.WriteLine("Cache item key: {0}", key);
            Console.WriteLine($"Setting value '{message}' in cache for 7 seconds");
            await cache.SetAsync(
                key,
                value,
                new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(7)));
            Console.WriteLine("Set");
            bool getFromCache = true;
            while (getFromCache)
            {
                var text = await cache.GetStringAsync(key);
                Console.WriteLine($"Getting value from cache: {text ?? "[NULL]"}");
                getFromCache = (text != null);

                await Task.Delay(10000);
            }
            value = await cache.GetAsync(key);
            if (value != null)
            {
                Console.WriteLine("Retrieved: " + Encoding.UTF8.GetString(value, 0, value.Length));
            }
            else
            {
                Console.WriteLine("Not Found");
            }

            Console.WriteLine("Refreshing value in cache");
            await cache.RefreshAsync(key);
            Console.WriteLine("Refreshed");

            Console.WriteLine("Removing value from cache");
            await cache.RemoveAsync(key);
            Console.WriteLine("Removed");

            Console.WriteLine("Getting value from cache again");
            value = await cache.GetAsync(key);
            if (value != null)
            {
                Console.WriteLine("Retrieved: " + Encoding.UTF8.GetString(value, 0, value.Length));
            }
            else
            {
                Console.WriteLine("Not Found");
            }

            Console.ReadLine();
        }
    }
}
