using Microsoft.Extensions.Logging;
using Community.Microsoft.Extensions.Caching.PostgreSql;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
using Testcontainers.PostgreSql;

namespace CachingTest;

/// <summary>
/// Tests for expiration validation edge cases.
/// These tests verify that invalid expiration configurations are properly rejected.
/// </summary>
public class ExpirationEdgeCaseTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private readonly ILogger<DatabaseOperations> _logger = new NullLoggerFactory().CreateLogger<DatabaseOperations>();
    private PostgreSqlCacheOptions _options = null!;

    public ExpirationEdgeCaseTests()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:latest")
            .WithDatabase("testdb")
            .WithUsername("testuser")
            .WithPassword("testpass")
            .WithCleanUp(true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        // Initialize options once for all tests
        _options = new PostgreSqlCacheOptions
        {
            ConnectionString = _postgresContainer.GetConnectionString(),
            SchemaName = "cache",
            TableName = "distributed_cache",
            CreateInfrastructure = false
        };
    }

    public async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
    }

    [Fact]
    public void SetCacheItem_WithZeroSlidingExpiration_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var dbOperations = new DatabaseOperations(Options.Create(_options), _logger);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            dbOperations.SetCacheItem("test-key", new byte[] { 1, 2, 3 },
                new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.Zero }));
    }

    [Fact]
    public void SetCacheItem_WithNegativeSlidingExpiration_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var dbOperations = new DatabaseOperations(Options.Create(_options), _logger);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            dbOperations.SetCacheItem("test-key", new byte[] { 1, 2, 3 },
                new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(-1) }));
    }

    [Fact]
    public void SetCacheItem_WithNoExpiration_ThrowsInvalidOperationException()
    {
        // Arrange
        var dbOperations = new DatabaseOperations(Options.Create(_options), _logger);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            dbOperations.SetCacheItem("test-key", new byte[] { 1, 2, 3 },
                new DistributedCacheEntryOptions()));
    }

    [Fact]
    public void SetCacheItem_WithPastAbsoluteExpiration_ThrowsInvalidOperationException()
    {
        // Arrange
        var dbOperations = new DatabaseOperations(Options.Create(_options), _logger);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            dbOperations.SetCacheItem("test-key", new byte[] { 1, 2, 3 },
                new DistributedCacheEntryOptions { AbsoluteExpiration = DateTime.UtcNow.AddMinutes(-1) }));
    }

    [Fact]
    public void SetCacheItem_WithNegativeAbsoluteExpirationRelativeToNow_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var dbOperations = new DatabaseOperations(Options.Create(_options), _logger);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            dbOperations.SetCacheItem("test-key", new byte[] { 1, 2, 3 },
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(-1) }));
    }

    [Fact]
    public void SetCacheItem_WithCurrentTimeAbsoluteExpiration_ThrowsInvalidOperationException()
    {
        // Arrange
        var dbOperations = new DatabaseOperations(Options.Create(_options), _logger);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            dbOperations.SetCacheItem("test-key", new byte[] { 1, 2, 3 },
                new DistributedCacheEntryOptions { AbsoluteExpiration = DateTime.UtcNow }));
    }

    [Fact]
    public void SetCacheItem_WithZeroAbsoluteExpirationRelativeToNow_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var dbOperations = new DatabaseOperations(Options.Create(_options), _logger);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            dbOperations.SetCacheItem("test-key", new byte[] { 1, 2, 3 },
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.Zero }));
    }
}