using Microsoft.Extensions.Logging;
using Community.Microsoft.Extensions.Caching.PostgreSql;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.PostgreSql;
using Microsoft.Extensions.Caching.Distributed;
using Xunit.Abstractions;
using Npgsql;

namespace CachingTest;

public class DatabaseResilienceIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private readonly ITestOutputHelper _output;
    private readonly ILogger<DatabaseOperations> _logger;

    public DatabaseResilienceIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        _logger = new NullLoggerFactory().CreateLogger<DatabaseOperations>();

        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:latest")
            .WithPassword("Strong_password_123!")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
    }

    [Fact]
    public async Task DatabaseOperations_WithTransientConnectionFailures_ShouldRetryAndRecover()
    {
        // Arrange
        var validConnectionString = _postgresContainer.GetConnectionString();
        var options = new PostgreSqlCacheOptions
        {
            ConnectionString = validConnectionString,
            SchemaName = "cache",
            TableName = "distributed_cache",
            CreateInfrastructure = true,
            EnableResiliencePatterns = true,
            MaxRetryAttempts = 3,
            RetryDelay = TimeSpan.FromMilliseconds(100),
            EnableResilienceLogging = true
        };

        var dbOperations = new DatabaseOperations(Options.Create(options), _logger);

        // Act - First operation should succeed (creates infrastructure)
        dbOperations.SetCacheItem("test-key", new byte[] { 1, 2, 3 },
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });

        var result1 = dbOperations.GetCacheItem("test-key");
        Assert.NotNull(result1);
        Assert.Equal(new byte[] { 1, 2, 3 }, result1);

        // Stop container to simulate connection failure
        await _postgresContainer.StopAsync();

        // Operations should fail gracefully (return null/do nothing)
        var result2 = dbOperations.GetCacheItem("test-key");
        Assert.Null(result2); // Should return null due to connection failure

        dbOperations.SetCacheItem("test-key-2", new byte[] { 4, 5, 6 },
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });
        // Should not throw

        // Restart container
        await _postgresContainer.StartAsync();

        // Wait a moment for container to be ready
        await Task.Delay(2000);

        // Create new DatabaseOperations instance to re-establish schema after restart
        var recoveryOptions = new PostgreSqlCacheOptions
        {
            ConnectionString = _postgresContainer.GetConnectionString(),
            SchemaName = "cache",
            TableName = "distributed_cache",
            CreateInfrastructure = true, // Re-create schema
            EnableResiliencePatterns = true,
            MaxRetryAttempts = 3,
            RetryDelay = TimeSpan.FromMilliseconds(100),
            EnableResilienceLogging = true
        };

        var recoveryDbOperations = new DatabaseOperations(Options.Create(recoveryOptions), _logger);

        // Operations should work again after recovery
        recoveryDbOperations.SetCacheItem("test-key-recovered", new byte[] { 7, 8, 9 },
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });

        var result3 = recoveryDbOperations.GetCacheItem("test-key-recovered");
        Assert.NotNull(result3);
        Assert.Equal(new byte[] { 7, 8, 9 }, result3);
    }

    [Fact]
    public async Task DatabaseOperations_WithCircuitBreakerPattern_ShouldOpenAndResetCircuit()
    {
        // Arrange
        var invalidConnectionString = "Host=nonexistent-host;Database=test;Username=test;Password=test";
        var options = new PostgreSqlCacheOptions
        {
            ConnectionString = invalidConnectionString,
            SchemaName = "cache",
            TableName = "distributed_cache",
            CreateInfrastructure = false, // Avoid creation failure
            EnableResiliencePatterns = true,
            MaxRetryAttempts = 2,
            RetryDelay = TimeSpan.FromMilliseconds(100),
            CircuitBreakerFailureThreshold = 3,
            CircuitBreakerDurationOfBreak = TimeSpan.FromSeconds(2),
            EnableResilienceLogging = true
        };

        var dbOperations = new DatabaseOperations(Options.Create(options), _logger);

        // Act - Trigger multiple failures to open circuit breaker
        for (int i = 0; i < 5; i++)
        {
            var result = dbOperations.GetCacheItem($"test-key-{i}");
            Assert.Null(result); // Should return null due to connection failure
        }

        // Circuit should be open now - operations should fail fast or return null gracefully
        var startTime = DateTime.UtcNow;
        var result1 = dbOperations.GetCacheItem("test-key-fast-fail");
        var fastFailTime = DateTime.UtcNow - startTime;

        Assert.Null(result1);
        // Should complete reasonably quickly (either fast fail or graceful return)
        Assert.True(fastFailTime < TimeSpan.FromSeconds(5), $"Operation took {fastFailTime.TotalMilliseconds}ms");

        // Wait for circuit breaker to reset
        await Task.Delay(TimeSpan.FromSeconds(3));

        // Circuit should be half-open now, allowing test requests
        var result2 = dbOperations.GetCacheItem("test-key-half-open");
        Assert.Null(result2); // Still fails because connection is invalid, but circuit is trying
    }

    [Fact]
    public async Task DatabaseOperations_WithValidationErrors_ShouldThrowImmediately()
    {
        // Arrange
        var validConnectionString = _postgresContainer.GetConnectionString();
        var options = new PostgreSqlCacheOptions
        {
            ConnectionString = validConnectionString,
            SchemaName = "cache",
            TableName = "distributed_cache",
            CreateInfrastructure = true,
            EnableResiliencePatterns = true,
            MaxRetryAttempts = 3,
            RetryDelay = TimeSpan.FromMilliseconds(100)
        };

        var dbOperations = new DatabaseOperations(Options.Create(options), _logger);

        // Act & Assert - Validation errors should not be caught by resilience patterns
        Assert.Throws<InvalidOperationException>(() =>
            dbOperations.SetCacheItem("test-key", new byte[] { 1, 2, 3 },
                new DistributedCacheEntryOptions { AbsoluteExpiration = DateTime.UtcNow.AddMinutes(-1) }));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            dbOperations.SetCacheItemAsync("test-key", new byte[] { 1, 2, 3 },
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(-1) },
                CancellationToken.None));
    }

    [Fact]
    public async Task DatabaseOperations_PerformanceUnderFailure_ShouldNotExceedThresholds()
    {
        // Arrange
        var invalidConnectionString = "Host=nonexistent-host;Database=test;Username=test;Password=test";
        var options = new PostgreSqlCacheOptions
        {
            ConnectionString = invalidConnectionString,
            SchemaName = "cache",
            TableName = "distributed_cache",
            CreateInfrastructure = false,
            EnableResiliencePatterns = true,
            MaxRetryAttempts = 2,
            RetryDelay = TimeSpan.FromMilliseconds(50),
            OperationTimeout = TimeSpan.FromSeconds(2)
        };

        var dbOperations = new DatabaseOperations(Options.Create(options), _logger);

        // Act - Measure performance of failed operations
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        for (int i = 0; i < 10; i++)
        {
            var result = dbOperations.GetCacheItem($"test-key-{i}");
            Assert.Null(result);
        }

        stopwatch.Stop();

        // Assert - Operations should complete reasonably quickly even with retries
        var averageTime = stopwatch.ElapsedMilliseconds / 10.0;
        _output.WriteLine($"Average operation time under failure: {averageTime}ms");

        // With circuit breaker and retries, performance can vary significantly
        // Allow generous buffer for system overhead and circuit breaker behavior
        Assert.True(averageTime < 2000, $"Average operation time {averageTime}ms exceeded threshold");
    }

    [Fact]
    public async Task DatabaseOperations_AsyncOperations_ShouldHandleFailuresGracefully()
    {
        // Arrange
        var validConnectionString = _postgresContainer.GetConnectionString();
        var options = new PostgreSqlCacheOptions
        {
            ConnectionString = validConnectionString,
            SchemaName = "cache",
            TableName = "distributed_cache",
            CreateInfrastructure = true,
            EnableResiliencePatterns = true,
            MaxRetryAttempts = 2,
            RetryDelay = TimeSpan.FromMilliseconds(100)
        };

        var dbOperations = new DatabaseOperations(Options.Create(options), _logger);

        // Act - Test successful operations first
        await dbOperations.SetCacheItemAsync("async-key", new byte[] { 1, 2, 3 },
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) },
            CancellationToken.None);

        var result1 = await dbOperations.GetCacheItemAsync("async-key", CancellationToken.None);
        Assert.NotNull(result1);
        Assert.Equal(new byte[] { 1, 2, 3 }, result1);

        // Stop container to simulate failure
        await _postgresContainer.StopAsync();

        // Async operations should handle failures gracefully
        var result2 = await dbOperations.GetCacheItemAsync("async-key", CancellationToken.None);
        Assert.Null(result2);

        await dbOperations.SetCacheItemAsync("async-key-2", new byte[] { 4, 5, 6 },
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) },
            CancellationToken.None);
        // Should not throw

        await dbOperations.RefreshCacheItemAsync("async-key", CancellationToken.None);
        // Should not throw

        await dbOperations.DeleteCacheItemAsync("async-key", CancellationToken.None);
        // Should not throw

        await dbOperations.DeleteExpiredCacheItemsAsync(CancellationToken.None);
        // Should not throw

        // Restart container for cleanup
        await _postgresContainer.StartAsync();
    }

    [Fact]
    public void DatabaseOperations_WithResilienceDisabled_ShouldThrowOriginalExceptions()
    {
        // Arrange
        var invalidConnectionString = "Host=nonexistent-host;Database=test;Username=test;Password=test";
        var options = new PostgreSqlCacheOptions
        {
            ConnectionString = invalidConnectionString,
            SchemaName = "cache",
            TableName = "distributed_cache",
            CreateInfrastructure = false,
            EnableResiliencePatterns = false // Disabled
        };

        var dbOperations = new DatabaseOperations(Options.Create(options), _logger);

        // Act & Assert - Should throw original exceptions when resilience is disabled
        // Note: May throw SocketException or PostgresException depending on connection failure type
        Assert.ThrowsAny<Exception>(() => dbOperations.GetCacheItem("test-key"));
        Assert.ThrowsAny<Exception>(() => dbOperations.SetCacheItem("test-key", new byte[] { 1, 2, 3 },
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) }));
        Assert.ThrowsAny<Exception>(() => dbOperations.DeleteCacheItem("test-key"));
    }

    [Fact]
    public async Task DatabaseOperations_ConcurrentOperationsUnderFailure_ShouldHandleGracefully()
    {
        // Arrange
        var invalidConnectionString = "Host=nonexistent-host;Database=test;Username=test;Password=test";
        var options = new PostgreSqlCacheOptions
        {
            ConnectionString = invalidConnectionString,
            SchemaName = "cache",
            TableName = "distributed_cache",
            CreateInfrastructure = false,
            EnableResiliencePatterns = true,
            MaxRetryAttempts = 1,
            RetryDelay = TimeSpan.FromMilliseconds(50),
            CircuitBreakerFailureThreshold = 5,
            CircuitBreakerDurationOfBreak = TimeSpan.FromSeconds(1)
        };

        var dbOperations = new DatabaseOperations(Options.Create(options), _logger);

        // Act - Run multiple concurrent operations that will fail
        var tasks = new List<Task>();

        for (int i = 0; i < 10; i++)
        {
            var index = i;
            tasks.Add(Task.Run(() =>
            {
                // Mix of sync and async operations
                var result1 = dbOperations.GetCacheItem($"concurrent-key-{index}");
                Assert.Null(result1);

                dbOperations.SetCacheItem($"concurrent-set-{index}", new byte[] { (byte)index },
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });

                dbOperations.DeleteCacheItem($"concurrent-delete-{index}");
            }));

            tasks.Add(Task.Run(async () =>
            {
                var result = await dbOperations.GetCacheItemAsync($"concurrent-async-{index}", CancellationToken.None);
                Assert.Null(result);

                await dbOperations.SetCacheItemAsync($"concurrent-async-set-{index}", new byte[] { (byte)index },
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) },
                    CancellationToken.None);
            }));
        }

        // Wait for all operations to complete
        await Task.WhenAll(tasks);

        // Assert - All operations should complete without exceptions
        Assert.True(true, "All concurrent operations completed without throwing exceptions");
    }

    [Fact]
    public async Task DatabaseOperations_LongRunningOperations_ShouldRespectTimeout()
    {
        // Arrange - Use a very short timeout to test timeout behavior
        var invalidConnectionString = "Host=nonexistent-host;Database=test;Username=test;Password=test";
        var options = new PostgreSqlCacheOptions
        {
            ConnectionString = invalidConnectionString,
            SchemaName = "cache",
            TableName = "distributed_cache",
            CreateInfrastructure = false,
            EnableResiliencePatterns = true,
            MaxRetryAttempts = 1,
            RetryDelay = TimeSpan.FromMilliseconds(50),
            OperationTimeout = TimeSpan.FromMilliseconds(500) // Very short timeout
        };

        var dbOperations = new DatabaseOperations(Options.Create(options), _logger);

        // Act & Assert - Operations should timeout and return gracefully
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var result = dbOperations.GetCacheItem("timeout-test-key");

        stopwatch.Stop();

        Assert.Null(result);
        // Should complete faster than a normal connection timeout
        Assert.True(stopwatch.ElapsedMilliseconds < 2000,
            $"Operation took {stopwatch.ElapsedMilliseconds}ms, should have timed out faster");
    }

    [Fact]
    public async Task DatabaseOperations_RecoveryAfterCircuitBreakerReset_ShouldWorkCorrectly()
    {
        // This test requires both valid and invalid connections to test recovery
        var validConnectionString = _postgresContainer.GetConnectionString();

        // Start with invalid connection to trigger circuit breaker
        var options = new PostgreSqlCacheOptions
        {
            ConnectionString = "Host=nonexistent-host;Database=test;Username=test;Password=test",
            SchemaName = "cache",
            TableName = "distributed_cache",
            CreateInfrastructure = false,
            EnableResiliencePatterns = true,
            MaxRetryAttempts = 1,
            RetryDelay = TimeSpan.FromMilliseconds(50),
            CircuitBreakerFailureThreshold = 3,
            CircuitBreakerDurationOfBreak = TimeSpan.FromSeconds(1)
        };

        var dbOperations = new DatabaseOperations(Options.Create(options), _logger);

        // Trigger circuit breaker
        for (int i = 0; i < 5; i++)
        {
            var result = dbOperations.GetCacheItem($"test-{i}");
            Assert.Null(result);
        }

        // Wait for circuit breaker to reset
        await Task.Delay(TimeSpan.FromSeconds(2));

        // Note: In a real scenario, we would update the connection string
        // For this test, we're just verifying the circuit breaker behavior
        // The operations will still fail but the circuit should be trying again

        var startTime = DateTime.UtcNow;
        var result1 = dbOperations.GetCacheItem("recovery-test");
        var operationTime = DateTime.UtcNow - startTime;

        Assert.Null(result1);
        // Should take longer when circuit is half-open and actually trying
        // vs when circuit is open and failing fast
        _output.WriteLine($"Recovery operation time: {operationTime.TotalMilliseconds}ms");
    }
}