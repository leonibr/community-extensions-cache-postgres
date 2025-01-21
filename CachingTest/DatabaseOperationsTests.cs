
using Microsoft.Extensions.Logging;
using Community.Microsoft.Extensions.Caching.PostgreSql;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.PostgreSql;
using Microsoft.Extensions.Caching.Distributed;

namespace CachingTest;


public class DatabaseOperationsTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private DatabaseOperations? _databaseOperations;
    private readonly PostgreSqlCacheOptions _options;
    private readonly ILogger<DatabaseOperations> _logger = new NullLoggerFactory().CreateLogger<DatabaseOperations>();

    public DatabaseOperationsTests()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:latest")
            .WithPassword("Strong_password_123!")
            .Build();

        _options = new PostgreSqlCacheOptions
        {
            ConnectionString = string.Empty, // Will be set after container starts
            SchemaName = "cache",
            TableName = "distributed_cache",
            CreateInfrastructure = true
        };
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        _options.ConnectionString = _postgresContainer.GetConnectionString();
        _databaseOperations = new DatabaseOperations(Options.Create(_options), _logger);
    }

    public async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
    }


    [Fact]
    public async Task DeleteExpiredCacheItems_Should_Remove_Expired_Items()
    {
        // Arrange
        await InitializeAsync();
        var key = "expired-test";
        var value = new byte[] { 1, 2, 3 };
        var expiresAtTime = DateTime.UtcNow.AddMilliseconds(300);

        // Act
        if (_databaseOperations == null)
        {
            throw new Exception("DatabaseOperations is null");
        }
        await _databaseOperations.SetCacheItemAsync(key, value, new DistributedCacheEntryOptions { AbsoluteExpiration = expiresAtTime }, CancellationToken.None);
        var beforeDelete = await _databaseOperations.GetCacheItemAsync(key, CancellationToken.None);
        await Task.Delay(400);
        await _databaseOperations.DeleteExpiredCacheItemsAsync(CancellationToken.None);
        var afterDelete = await _databaseOperations.GetCacheItemAsync(key, CancellationToken.None);

        // Assert
        Assert.NotNull(beforeDelete);
        Assert.Null(afterDelete);
    }

    [Fact]
    public async Task SetAndGet_CacheItem_Should_Work()
    {
        // Arrange
        await InitializeAsync();
        var key = "test-key";
        var expectedValue = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        var expiresAt = DateTime.UtcNow.AddMinutes(5);
        var result = await _databaseOperations.GetCacheItemAsync(key, CancellationToken.None);

        await _databaseOperations.SetCacheItemAsync(key, expectedValue, new DistributedCacheEntryOptions { AbsoluteExpiration = expiresAt }, CancellationToken.None);
        result = await _databaseOperations.GetCacheItemAsync(key, CancellationToken.None);
        Assert.Equal(expectedValue, result);
    }
}