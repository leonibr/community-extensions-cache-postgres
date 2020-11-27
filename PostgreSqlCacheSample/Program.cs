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
using System.Diagnostics;

namespace PostgreSqlCacheSample
{
    /// <summary>
    /// This sample requires setting up a PostgreSQL database called 'cache_test'.
    /// Execute the command 'prepare-database.cmd -create' at the root of the project
    /// Then run this sample by doing "dotnet run"
    /// </summary>
    public class Program
    {
        public static async Task Main()
        {
            await RunSampleAsync();
        }

        public static async Task RunSampleAsync()
        {
            var configurationBuilder = new ConfigurationBuilder();
            var configuration = configurationBuilder
                .AddJsonFile("config.json")
                .AddEnvironmentVariables()
                .Build();

            var key = Guid.NewGuid().ToString();
            var message = "Hello AbsoluteExpiration Value!";
            var value = Encoding.UTF8.GetBytes(message);

            Console.WriteLine("Connecting to cache");
            var cache = new PostgreSqlCache(new PostgreSqlCacheOptions()
            {
                ConnectionString = configuration["ConnectionString"],
                SchemaName = configuration["SchemaName"],
                TableName = configuration["TableName"],
            });

            Console.WriteLine("Connected\n");

            Console.WriteLine($"Cache item key: {key}\nSetting value '{message}' in cache for 7 seconds [AbsoluteExpiration]");
            await cache.SetAsync(
                key,
                value,
                new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(7)));
            Console.WriteLine("Value stored!\n");

            async Task getKeyStatement()
            {
                Stopwatch sp = new Stopwatch();
                Console.Write($"\tGetting Key: {key}\n\t");
                sp.Start();
                byte[] rawBytes = await cache.GetAsync(key);
                if (rawBytes != null)
                {
                    Console.WriteLine($"Retrieved ({sp.ElapsedMilliseconds}ms): {Encoding.UTF8.GetString(rawBytes, 0, rawBytes.Length)}\n");
                }
                else
                {
                    Console.WriteLine($"Not Found({sp.ElapsedMilliseconds}ms)\n");
                }
            }
            async Task waitFor(int seconds)
            {
                Console.WriteLine($"Waits {seconds} seconds\n");
                await Task.Delay(TimeSpan.FromSeconds(seconds));
            }

            await getKeyStatement();
            await waitFor(4);
            await getKeyStatement();
            await waitFor(5);
            await getKeyStatement();
            Console.Write("Removing value from cache...");
            await cache.RemoveAsync(key);
            Console.WriteLine("Removed");

            key = Guid.NewGuid().ToString();
            message = "Hello SlidingExpiration Value!";
            value = Encoding.UTF8.GetBytes(message);

            Console.WriteLine($"\n\nCache item key: {key}\nSetting value '{message}' in cache for 7 seconds [SlidingExpiration]");
            await cache.SetAsync(
                key,
                value,
                new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(7)));
            Console.WriteLine("Value stored!\n");
            await getKeyStatement();
            await waitFor(2);
            Console.Write("Refreshing value ...");
            await cache.RefreshAsync(key);
            Console.WriteLine("Refreshed\n");
            await getKeyStatement();
            await waitFor(6);
            await getKeyStatement();


            await getKeyStatement();

            Console.Write("Removing value from cache...");
            await cache.RemoveAsync(key);
            Console.WriteLine("Removed");
            await getKeyStatement();
            Console.WriteLine("\nList all keys from database");
            using var conn = new Npgsql.NpgsqlConnection(configuration["ConnectionString"]);
            using var cmd = new Npgsql.NpgsqlCommand($"select \"Id\" from {configuration["SchemaName"]}.{configuration["TableName"]}", conn);
            conn.Open();
            var reader = cmd.ExecuteReader();
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    Console.WriteLine($"\t{reader["Id"]}");
                }

            }
            else
            {
                Console.WriteLine("\tNo data.");
            }
            reader.Close();
            conn.Close();
            Console.WriteLine("Sample project executed!");
            Console.ReadLine();
        }
    }
}
