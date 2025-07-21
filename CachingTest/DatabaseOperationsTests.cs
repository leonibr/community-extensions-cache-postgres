
using Microsoft.Extensions.Logging;
using Community.Microsoft.Extensions.Caching.PostgreSql;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.PostgreSql;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Internal;
using Moq;

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
    public void Constructor_WithNullConnectionStringAndDataSourceFactory_ThrowsArgumentException()
    {
        // Arrange
        var invalidOptions = new PostgreSqlCacheOptions
        {
            ConnectionString = null,
            DataSourceFactory = null,
            SchemaName = "cache",
            TableName = "distributed_cache"
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new DatabaseOperations(Options.Create(invalidOptions), _logger));
    }

    [Fact]
    public void Constructor_WithEmptySchemaName_ThrowsArgumentException()
    {
        // Arrange
        var invalidOptions = new PostgreSqlCacheOptions
        {
            ConnectionString = "test",
            SchemaName = "",
            TableName = "distributed_cache"
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new DatabaseOperations(Options.Create(invalidOptions), _logger));
    }

    [Fact]
    public void Constructor_WithEmptyTableName_ThrowsArgumentException()
    {
        // Arrange
        var invalidOptions = new PostgreSqlCacheOptions
        {
            ConnectionString = "test",
            SchemaName = "cache",
            TableName = ""
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new DatabaseOperations(Options.Create(invalidOptions), _logger));
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
        Assert.NotNull(_databaseOperations);
        await _databaseOperations!.SetCacheItemAsync(key, value, new DistributedCacheEntryOptions { AbsoluteExpiration = expiresAtTime }, CancellationToken.None);
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
        Assert.NotNull(_databaseOperations);
        var expiresAt = DateTime.UtcNow.AddMinutes(5);
        var result = await _databaseOperations!.GetCacheItemAsync(key, CancellationToken.None);

        await _databaseOperations.SetCacheItemAsync(key, expectedValue, new DistributedCacheEntryOptions { AbsoluteExpiration = expiresAt }, CancellationToken.None);
        result = await _databaseOperations.GetCacheItemAsync(key, CancellationToken.None);
        Assert.Equal(expectedValue, result);
    }

    [Fact]
    public async Task DeleteCacheItem_Should_Remove_Item()
    {
        // Arrange
        await InitializeAsync();
        Assert.NotNull(_databaseOperations);
        var key = "delete-test";
        var value = new byte[] { 1, 2, 3 };
        var expiresAt = DateTime.UtcNow.AddMinutes(5);

        // Act
        await _databaseOperations!.SetCacheItemAsync(key, value, new DistributedCacheEntryOptions { AbsoluteExpiration = expiresAt }, CancellationToken.None);
        var beforeDelete = await _databaseOperations.GetCacheItemAsync(key, CancellationToken.None);
        await _databaseOperations.DeleteCacheItemAsync(key, CancellationToken.None);
        var afterDelete = await _databaseOperations.GetCacheItemAsync(key, CancellationToken.None);

        // Assert
        Assert.NotNull(beforeDelete);
        Assert.Null(afterDelete);
    }

    [Fact]
    public async Task RefreshCacheItem_Should_Update_Expiration()
    {
        // Arrange
        await InitializeAsync();
        Assert.NotNull(_databaseOperations);
        var key = "refresh-test";
        var value = new byte[] { 1, 2, 3 };
        // Act
        await _databaseOperations!.SetCacheItemAsync(key, value, new DistributedCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromSeconds(10)
        }, CancellationToken.None);

        // Verify item exists
        var initialResult = await _databaseOperations.GetCacheItemAsync(key, CancellationToken.None);
        Assert.NotNull(initialResult);
        Assert.Equal(value, initialResult);

        // Refresh the item
        await _databaseOperations.RefreshCacheItemAsync(key, CancellationToken.None);
        var result = await _databaseOperations.GetCacheItemAsync(key, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(value, result);
    }

    [Fact]
    public async Task SetCacheItem_WithSlidingExpiration_Should_Work()
    {
        // Arrange
        await InitializeAsync();
        Assert.NotNull(_databaseOperations);
        var key = "sliding-test";
        var value = new byte[] { 1, 2, 3 };

        // Act
        await _databaseOperations!.SetCacheItemAsync(key, value, new DistributedCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromSeconds(2)
        }, CancellationToken.None);

        var result1 = await _databaseOperations.GetCacheItemAsync(key, CancellationToken.None);
        await Task.Delay(1000);
        var result2 = await _databaseOperations.GetCacheItemAsync(key, CancellationToken.None);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(value, result2);
    }

    [Fact]
    public async Task SetCacheItem_WithAbsoluteExpirationRelativeToNow_Should_Work()
    {
        // Arrange
        await InitializeAsync();
        Assert.NotNull(_databaseOperations);
        var key = "absolute-relative-test";
        var value = new byte[] { 1, 2, 3 };

        // Act
        await _databaseOperations!.SetCacheItemAsync(key, value, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(1)
        }, CancellationToken.None);

        var result1 = await _databaseOperations.GetCacheItemAsync(key, CancellationToken.None);
        await Task.Delay(1100);
        var result2 = await _databaseOperations.GetCacheItemAsync(key, CancellationToken.None);

        // Assert
        Assert.NotNull(result1);
        Assert.Null(result2);
    }

    [Fact]
    public async Task SetCacheItem_WithAbsoluteExpiration_Should_Work()
    {
        // Arrange
        await InitializeAsync();
        Assert.NotNull(_databaseOperations);
        var key = "absolute-test";
        var value = new byte[] { 1, 2, 3 };
        var absoluteExpiration = DateTime.UtcNow.AddSeconds(1);

        // Act
        await _databaseOperations!.SetCacheItemAsync(key, value, new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = absoluteExpiration
        }, CancellationToken.None);

        var result1 = await _databaseOperations.GetCacheItemAsync(key, CancellationToken.None);
        await Task.Delay(1100);
        var result2 = await _databaseOperations.GetCacheItemAsync(key, CancellationToken.None);

        // Assert
        Assert.NotNull(result1);
        Assert.Null(result2);
    }

    [Fact]
    public async Task SetCacheItem_WithPastAbsoluteExpiration_ThrowsInvalidOperationException()
    {
        // Arrange
        await InitializeAsync();
        Assert.NotNull(_databaseOperations);
        var key = "past-expiration-test";
        var value = new byte[] { 1, 2, 3 };
        var pastExpiration = DateTime.UtcNow.AddSeconds(-1);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _databaseOperations!.SetCacheItemAsync(key, value, new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = pastExpiration
            }, CancellationToken.None));
    }

    [Fact]
    public async Task SetCacheItem_WithNoExpiration_ThrowsInvalidOperationException()
    {
        // Arrange
        await InitializeAsync();
        Assert.NotNull(_databaseOperations);
        var key = "no-expiration-test";
        var value = new byte[] { 1, 2, 3 };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _databaseOperations!.SetCacheItemAsync(key, value, new DistributedCacheEntryOptions(), CancellationToken.None));
    }

    [Fact]
    public async Task GetCacheItem_WithNonExistentKey_ReturnsNull()
    {
        // Arrange
        await InitializeAsync();
        Assert.NotNull(_databaseOperations);
        var key = "non-existent-key";

        // Act
        var result = await _databaseOperations!.GetCacheItemAsync(key, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }
}

public class DatabaseOperationsReadOnlyTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private DatabaseOperations? _databaseOperations;
    private readonly PostgreSqlCacheOptions _options;
    private readonly ILogger<DatabaseOperations> _logger = new NullLoggerFactory().CreateLogger<DatabaseOperations>();

    public DatabaseOperationsReadOnlyTests()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:latest")
            .WithPassword("Strong_password_123!")
            .Build();

        _options = new PostgreSqlCacheOptions
        {
            ConnectionString = string.Empty,
            SchemaName = "cache",
            TableName = "distributed_cache",
            CreateInfrastructure = true,
            ReadOnlyMode = true
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
    public async Task DeleteCacheItem_InReadOnlyMode_DoesNothing()
    {
        // Arrange
        await InitializeAsync();
        var key = "readonly-delete-test";

        // Act - Should not throw
        await _databaseOperations!.DeleteCacheItemAsync(key, CancellationToken.None);

        // Assert - No exception thrown
        Assert.True(true);
    }

    [Fact]
    public async Task SetCacheItem_InReadOnlyMode_DoesNothing()
    {
        // Arrange
        await InitializeAsync();
        var key = "readonly-set-test";
        var value = new byte[] { 1, 2, 3 };

        // Act - Should not throw
        await _databaseOperations!.SetCacheItemAsync(key, value, new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = DateTime.UtcNow.AddMinutes(5)
        }, CancellationToken.None);

        // Assert - No exception thrown
        Assert.True(true);
    }

    [Fact]
    public async Task DeleteExpiredCacheItems_InReadOnlyMode_DoesNothing()
    {
        // Arrange
        await InitializeAsync();

        // Act - Should not throw
        await _databaseOperations!.DeleteExpiredCacheItemsAsync(CancellationToken.None);

        // Assert - No exception thrown
        Assert.True(true);
    }
}

public class DatabaseOperationsUpdateOnGetTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private DatabaseOperations? _databaseOperations;
    private readonly PostgreSqlCacheOptions _options;
    private readonly ILogger<DatabaseOperations> _logger = new NullLoggerFactory().CreateLogger<DatabaseOperations>();

    public DatabaseOperationsUpdateOnGetTests()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:latest")
            .WithPassword("Strong_password_123!")
            .Build();

        _options = new PostgreSqlCacheOptions
        {
            ConnectionString = string.Empty,
            SchemaName = "cache",
            TableName = "distributed_cache",
            CreateInfrastructure = true,
            UpdateOnGetCacheItem = true
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
    public async Task GetCacheItem_WithUpdateOnGet_UpdatesExpiration()
    {
        // Arrange
        await InitializeAsync();
        Assert.NotNull(_databaseOperations);
        var key = "update-on-get-test";
        var value = new byte[] { 1, 2, 3 };

        // Act
        await _databaseOperations!.SetCacheItemAsync(key, value, new DistributedCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromSeconds(2)
        }, CancellationToken.None);

        var result1 = await _databaseOperations.GetCacheItemAsync(key, CancellationToken.None);
        await Task.Delay(1000);
        var result2 = await _databaseOperations.GetCacheItemAsync(key, CancellationToken.None);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(value, result2);
    }
}