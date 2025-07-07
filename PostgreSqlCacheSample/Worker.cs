using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;

namespace PostgreSqlCacheSample
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IDistributedCache _cache;
        private readonly IConfiguration _configuration;

        public Worker(ILogger<Worker> logger, IDistributedCache cache, IConfiguration configuration)
        {
            _logger = logger;
            _cache = cache;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var key = Guid.NewGuid().ToString();
            var message = "Hello AbsoluteExpiration Value!";
            var value = Encoding.UTF8.GetBytes(message);

            async Task GetKeyStatement()
            {
                Stopwatch sp = new Stopwatch();
                _logger.LogInformation($"\tGetting Key: {key}\n\t");
                sp.Start();
                byte[] rawBytes = await _cache.GetAsync(key);
                if (rawBytes != null)
                {
                    _logger.LogInformation($"Retrieved ({sp.ElapsedMilliseconds}ms): {Encoding.UTF8.GetString(rawBytes, 0, rawBytes.Length)}\n");
                }
                else
                {
                    _logger.LogInformation($"Not Found({sp.ElapsedMilliseconds}ms)\n");
                }
            }

            async Task WaitFor(int seconds)
            {
                _logger.LogInformation($"Waits {seconds} seconds\n");
                await Task.Delay(TimeSpan.FromSeconds(seconds));
            }



            _logger.LogInformation("Connecting to cache");

            _logger.LogInformation("Connected\n");

            _logger.LogInformation($"Cache item key: {key}\nSetting value '{message}' in cache for 7 seconds [AbsoluteExpiration]");
            await _cache.SetAsync(
                key,
                value,
                new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(7)));
            _logger.LogInformation("Value stored!\n");


            await GetKeyStatement();
            await WaitFor(4);
            await GetKeyStatement();
            await WaitFor(5);
            await GetKeyStatement();
            _logger.LogInformation("Removing value from cache...");
            await _cache.RemoveAsync(key);
            _logger.LogInformation("Removed");

            key = Guid.NewGuid().ToString();
            message = "Hello SlidingExpiration Value!";
            value = Encoding.UTF8.GetBytes(message);

            _logger.LogInformation($"\n\nCache item key: {key}\nSetting value '{message}' in cache for 7 seconds [SlidingExpiration]");
            await _cache.SetAsync(
                key,
                value,
                new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(7)));
            _logger.LogInformation("Value stored!\n");
            await GetKeyStatement();
            await WaitFor(2);
            _logger.LogInformation("Refreshing value ...");
            await _cache.RefreshAsync(key);
            _logger.LogInformation("Refreshed\n");
            await GetKeyStatement();
            await WaitFor(6);
            await GetKeyStatement();
            await GetKeyStatement();
            _logger.LogInformation("Removing value from cache...");
            await _cache.RemoveAsync(key);
            _logger.LogInformation("Removed");
            await GetKeyStatement();
            _logger.LogInformation("\nList all keys from database");
            await using var conn = new Npgsql.NpgsqlConnection(_configuration["ConnectionString"]);
            var schemaName = _configuration["SchemaName"];
            var tableName = _configuration["TableName"];
            
            // Validate identifier names to prevent SQL injection
            if (string.IsNullOrWhiteSpace(schemaName) || string.IsNullOrWhiteSpace(tableName) ||
                schemaName.Contains('"') || tableName.Contains('"') ||
                schemaName.Contains(';') || tableName.Contains(';'))
            {
                throw new InvalidOperationException("Invalid schema or table name configuration");
            }
            
            // Use proper identifier quoting for PostgreSQL
            var sql = $"SELECT \"Id\" FROM \"{schemaName}\".\"{tableName}\"";
            await using var cmd = new Npgsql.NpgsqlCommand(sql, conn);
            conn.Open();
            var reader = cmd.ExecuteReader();
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    _logger.LogInformation($"\t{reader["Id"]}");
                }
            }
            else
            {
                _logger.LogInformation("\tNo data.");
            }
            reader.Close();
            conn.Close();
            _logger.LogInformation("Sample project executed!");
        }
    }
}
