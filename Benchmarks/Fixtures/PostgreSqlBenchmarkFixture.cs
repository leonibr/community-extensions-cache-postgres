using System.Data.Common;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using Community.Microsoft.Extensions.Caching.PostgreSql;
using DotNet.Testcontainers.Builders;

namespace Benchmarks.Fixtures;

/// <summary>
/// PostgreSQL TestContainer fixture for benchmarking
/// </summary>
public class PostgreSqlBenchmarkFixture : IAsyncDisposable
{
    private readonly PostgreSqlContainer _container;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PostgreSqlBenchmarkFixture> _logger;

    public PostgreSqlBenchmarkFixture()
    {
        // Create PostgreSQL container
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16")
            .WithDatabase("benchmark_db")
            .WithUsername("benchmark_user")
            .WithPassword("benchmark_password")
            .WithPortBinding(5432, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
            .Build();

        // Setup service provider
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));

        _serviceProvider = services.BuildServiceProvider();
        _logger = _serviceProvider.GetRequiredService<ILogger<PostgreSqlBenchmarkFixture>>();
    }

    /// <summary>
    /// Gets the connection string for the PostgreSQL container
    /// </summary>
    public string ConnectionString => _container.GetConnectionString();

    /// <summary>
    /// Gets the PostgreSQL container instance
    /// </summary>
    public PostgreSqlContainer Container => _container;

    /// <summary>
    /// Initializes the container and creates a distributed cache instance
    /// </summary>
    public async Task<IDistributedCache> InitializeAsync()
    {
        // Start the container
        await _container.StartAsync();

        // Create service collection and configure PostgreSQL cache
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));

        services.AddDistributedPostgreSqlCache(options =>
        {
            options.ConnectionString = ConnectionString;
            options.SchemaName = "benchmark_cache";
            options.TableName = "cache_items";
            options.CreateInfrastructure = true;
            options.DefaultSlidingExpiration = TimeSpan.FromMinutes(20);
            options.ExpiredItemsDeletionInterval = TimeSpan.FromMinutes(5);
        });

        var serviceProvider = services.BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<IDistributedCache>();

        // Ensure the cache is properly initialized
        await cache.SetStringAsync("init_key", "init_value");
        await cache.RemoveAsync("init_key");

        return cache;
    }

    /// <summary>
    /// Creates a new DbConnection to the PostgreSQL container
    /// </summary>
    public DbConnection CreateConnection()
    {
        var connection = new Npgsql.NpgsqlConnection(ConnectionString);
        return connection;
    }

    /// <summary>
    /// Cleans up the benchmark database by removing all cache items
    /// </summary>
    public async Task CleanupAsync()
    {
        try
        {
            using var connection = CreateConnection();
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM benchmark_cache.cache_items;";
            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cleanup benchmark database");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
        if (_serviceProvider is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}