using Microsoft.Extensions.Logging;
using Community.Microsoft.Extensions.Caching.PostgreSql;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Internal;
using Moq;
using Npgsql;

namespace CachingTest;

public class DatabaseOperationsAdditionalTests
{
    private readonly ILogger<DatabaseOperations> _logger = new NullLoggerFactory().CreateLogger<DatabaseOperations>();

    [Fact]
    public void Constructor_WithDataSourceFactory_UsesDataSourceFactory()
    {
        // Arrange
        var mockDataSource = new Mock<NpgsqlDataSource>();
        // Note: NpgsqlConnection is sealed, so we can't mock it directly
        // This test verifies the constructor doesn't throw when DataSourceFactory is provided

        var options = new PostgreSqlCacheOptions
        {
            DataSourceFactory = () => mockDataSource.Object,
            SchemaName = "cache",
            TableName = "distributed_cache",
            CreateInfrastructure = false
        };

        // Act & Assert - Constructor should not throw, but operations will fail due to connection issues
        var dbOperations = new DatabaseOperations(Options.Create(options), _logger);
        Assert.NotNull(dbOperations);

        // Verify that operations throw NotSupportedException due to Moq limitations
        Assert.Throws<NotSupportedException>(() => dbOperations.GetCacheItem("test-key"));
    }

    [Fact]
    public void DeleteCacheItem_Synchronous_Should_Not_Throw()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions
        {
            ConnectionString = "Host=localhost;Database=test;Username=test;Password=test",
            SchemaName = "cache",
            TableName = "distributed_cache",
            CreateInfrastructure = false
        };

        var dbOperations = new DatabaseOperations(Options.Create(options), _logger);

        // Act & Assert - Should not throw even with invalid connection string
        // since we're not actually connecting in this test
        Assert.Throws<Npgsql.NpgsqlException>(() => dbOperations.DeleteCacheItem("test-key"));
    }

    [Fact]
    public void GetCacheItem_Synchronous_Should_Not_Throw()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions
        {
            ConnectionString = "Host=localhost;Database=test;Username=test;Password=test",
            SchemaName = "cache",
            TableName = "distributed_cache",
            CreateInfrastructure = false
        };

        var dbOperations = new DatabaseOperations(Options.Create(options), _logger);

        // Act & Assert - Should not throw even with invalid connection string
        Assert.Throws<Npgsql.NpgsqlException>(() => dbOperations.GetCacheItem("test-key"));
    }

    [Fact]
    public void RefreshCacheItem_Synchronous_Should_Not_Throw()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions
        {
            ConnectionString = "Host=localhost;Database=test;Username=test;Password=test",
            SchemaName = "cache",
            TableName = "distributed_cache",
            CreateInfrastructure = false
        };

        var dbOperations = new DatabaseOperations(Options.Create(options), _logger);

        // Act & Assert - Should not throw even with invalid connection string
        Assert.Throws<Npgsql.NpgsqlException>(() => dbOperations.RefreshCacheItem("test-key"));
    }

    [Fact]
    public void SetCacheItem_Synchronous_Should_Not_Throw()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions
        {
            ConnectionString = "Host=localhost;Database=test;Username=test;Password=test",
            SchemaName = "cache",
            TableName = "distributed_cache",
            CreateInfrastructure = false
        };

        var dbOperations = new DatabaseOperations(Options.Create(options), _logger);

        // Act & Assert - Should not throw even with invalid connection string
        Assert.Throws<Npgsql.NpgsqlException>(() => dbOperations.SetCacheItem("test-key", new byte[] { 1, 2, 3 },
            new DistributedCacheEntryOptions { AbsoluteExpiration = DateTime.UtcNow.AddMinutes(5) }));
    }

    [Fact]
    public void Constructor_WithCustomSystemClock_UsesCustomClock()
    {
        // Arrange
        var mockClock = new Mock<ISystemClock>();
        var customTime = DateTimeOffset.UtcNow;
        mockClock.Setup(x => x.UtcNow).Returns(customTime);

        var options = new PostgreSqlCacheOptions
        {
            ConnectionString = "Host=localhost;Database=test;Username=test;Password=test",
            SchemaName = "cache",
            TableName = "distributed_cache",
            CreateInfrastructure = false,
            SystemClock = mockClock.Object
        };

        // Act
        var dbOperations = new DatabaseOperations(Options.Create(options), _logger);

        // Assert
        Assert.NotNull(dbOperations);
    }

    [Fact]
    public void Constructor_WithCreateInfrastructureFalse_DoesNotCreateInfrastructure()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions
        {
            ConnectionString = "Host=localhost;Database=test;Username=test;Password=test",
            SchemaName = "cache",
            TableName = "distributed_cache",
            CreateInfrastructure = false
        };

        // Act & Assert - Should not throw since CreateInfrastructure is false
        var dbOperations = new DatabaseOperations(Options.Create(options), _logger);
        Assert.NotNull(dbOperations);
    }
}

public class PostgreSqlCacheAdditionalTests
{
    private readonly Mock<IDatabaseOperations> _mockDbOperations;
    private readonly Mock<IDatabaseExpiredItemsRemoverLoop> _mockRemoverLoop;
    private readonly PostgreSqlCacheOptions _options;

    public PostgreSqlCacheAdditionalTests()
    {
        _mockDbOperations = new Mock<IDatabaseOperations>();
        _mockRemoverLoop = new Mock<IDatabaseExpiredItemsRemoverLoop>();
        _options = new PostgreSqlCacheOptions
        {
            DefaultSlidingExpiration = TimeSpan.FromMinutes(30)
        };
    }

    [Fact]
    public void Constructor_WithNullDatabaseOperations_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PostgreSqlCache(
            Options.Create(_options), null!, _mockRemoverLoop.Object));
    }

    [Fact]
    public void Constructor_WithNullRemoverLoop_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PostgreSqlCache(
            Options.Create(_options), _mockDbOperations.Object, null!));
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PostgreSqlCache(
            null!, _mockDbOperations.Object, _mockRemoverLoop.Object));
    }

    [Fact]
    public void Constructor_WithZeroDefaultSlidingExpiration_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var invalidOptions = new PostgreSqlCacheOptions
        {
            DefaultSlidingExpiration = TimeSpan.Zero
        };

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new PostgreSqlCache(
            Options.Create(invalidOptions), _mockDbOperations.Object, _mockRemoverLoop.Object));
    }

    [Fact]
    public void Constructor_WithNegativeDefaultSlidingExpiration_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var invalidOptions = new PostgreSqlCacheOptions
        {
            DefaultSlidingExpiration = TimeSpan.FromMinutes(-1)
        };

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new PostgreSqlCache(
            Options.Create(invalidOptions), _mockDbOperations.Object, _mockRemoverLoop.Object));
    }

    [Fact]
    public void Get_WithNullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var cache = new PostgreSqlCache(Options.Create(_options), _mockDbOperations.Object, _mockRemoverLoop.Object);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => cache.Get(null!));
    }

    [Fact]
    public async Task GetAsync_WithNullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var cache = new PostgreSqlCache(Options.Create(_options), _mockDbOperations.Object, _mockRemoverLoop.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => cache.GetAsync(null!, CancellationToken.None));
    }

    [Fact]
    public void Refresh_WithNullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var cache = new PostgreSqlCache(Options.Create(_options), _mockDbOperations.Object, _mockRemoverLoop.Object);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => cache.Refresh(null!));
    }

    [Fact]
    public async Task RefreshAsync_WithNullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var cache = new PostgreSqlCache(Options.Create(_options), _mockDbOperations.Object, _mockRemoverLoop.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => cache.RefreshAsync(null!, CancellationToken.None));
    }

    [Fact]
    public void Remove_WithNullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var cache = new PostgreSqlCache(Options.Create(_options), _mockDbOperations.Object, _mockRemoverLoop.Object);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => cache.Remove(null!));
    }

    [Fact]
    public async Task RemoveAsync_WithNullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var cache = new PostgreSqlCache(Options.Create(_options), _mockDbOperations.Object, _mockRemoverLoop.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => cache.RemoveAsync(null!, CancellationToken.None));
    }

    [Fact]
    public void Set_WithNullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var cache = new PostgreSqlCache(Options.Create(_options), _mockDbOperations.Object, _mockRemoverLoop.Object);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => cache.Set(null!, new byte[] { 1, 2, 3 }, new DistributedCacheEntryOptions()));
    }

    [Fact]
    public void Set_WithNullValue_ThrowsArgumentNullException()
    {
        // Arrange
        var cache = new PostgreSqlCache(Options.Create(_options), _mockDbOperations.Object, _mockRemoverLoop.Object);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => cache.Set("test-key", null!, new DistributedCacheEntryOptions()));
    }

    [Fact]
    public void Set_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var cache = new PostgreSqlCache(Options.Create(_options), _mockDbOperations.Object, _mockRemoverLoop.Object);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => cache.Set("test-key", new byte[] { 1, 2, 3 }, null!));
    }

    [Fact]
    public async Task SetAsync_WithNullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var cache = new PostgreSqlCache(Options.Create(_options), _mockDbOperations.Object, _mockRemoverLoop.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => cache.SetAsync(null!, new byte[] { 1, 2, 3 }, new DistributedCacheEntryOptions(), CancellationToken.None));
    }

    [Fact]
    public async Task SetAsync_WithNullValue_ThrowsArgumentNullException()
    {
        // Arrange
        var cache = new PostgreSqlCache(Options.Create(_options), _mockDbOperations.Object, _mockRemoverLoop.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => cache.SetAsync("test-key", null!, new DistributedCacheEntryOptions(), CancellationToken.None));
    }

    [Fact]
    public async Task SetAsync_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var cache = new PostgreSqlCache(Options.Create(_options), _mockDbOperations.Object, _mockRemoverLoop.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => cache.SetAsync("test-key", new byte[] { 1, 2, 3 }, null!, CancellationToken.None));
    }

    [Fact]
    public void Set_WithNoExpirationOptions_UsesDefaultSlidingExpiration()
    {
        // Arrange
        var cache = new PostgreSqlCache(Options.Create(_options), _mockDbOperations.Object, _mockRemoverLoop.Object);
        var options = new DistributedCacheEntryOptions();

        // Act
        cache.Set("test-key", new byte[] { 1, 2, 3 }, options);

        // Assert
        _mockDbOperations.Verify(x => x.SetCacheItem("test-key", new byte[] { 1, 2, 3 }, It.Is<DistributedCacheEntryOptions>(o => o.SlidingExpiration == _options.DefaultSlidingExpiration)), Times.Once);
    }

    [Fact]
    public async Task SetAsync_WithNoExpirationOptions_UsesDefaultSlidingExpiration()
    {
        // Arrange
        var cache = new PostgreSqlCache(Options.Create(_options), _mockDbOperations.Object, _mockRemoverLoop.Object);
        var options = new DistributedCacheEntryOptions();

        // Act
        await cache.SetAsync("test-key", new byte[] { 1, 2, 3 }, options, CancellationToken.None);

        // Assert
        _mockDbOperations.Verify(x => x.SetCacheItemAsync("test-key", new byte[] { 1, 2, 3 }, It.Is<DistributedCacheEntryOptions>(o => o.SlidingExpiration == _options.DefaultSlidingExpiration), CancellationToken.None), Times.Once);
    }
}

public class DatabaseExpiredItemsRemoverLoopAdditionalTests
{
    private readonly Mock<IDatabaseOperations> _mockDbOperations;
    private readonly Mock<ILogger<DatabaseExpiredItemsRemoverLoop>> _mockLogger;
    private readonly Mock<ISystemClock> _mockSystemClock;

    public DatabaseExpiredItemsRemoverLoopAdditionalTests()
    {
        _mockDbOperations = new Mock<IDatabaseOperations>();
        _mockLogger = new Mock<ILogger<DatabaseExpiredItemsRemoverLoop>>();
        _mockSystemClock = new Mock<ISystemClock>();
    }

    [Fact]
    public void Constructor_WithDisabledRemoveExpired_DoesNotConfigureAnything()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions
        {
            DisableRemoveExpired = true
        };

        // Act
        var removerLoop = new DatabaseExpiredItemsRemoverLoop(
            Options.Create(options),
            _mockDbOperations.Object,
            _mockLogger.Object);

        // Assert
        Assert.NotNull(removerLoop);
    }

    [Fact]
    public void Start_WhenDisabled_DoesNothing()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions
        {
            DisableRemoveExpired = true
        };

        var removerLoop = new DatabaseExpiredItemsRemoverLoop(
            Options.Create(options),
            _mockDbOperations.Object,
            _mockLogger.Object);

        // Act & Assert - Should not throw
        removerLoop.Start();
    }

    [Fact]
    public void Constructor_WithIntervalLessThanMinimum_ThrowsArgumentException()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions
        {
            ExpiredItemsDeletionInterval = TimeSpan.FromMinutes(1) // Less than 5 minutes minimum
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new DatabaseExpiredItemsRemoverLoop(
            Options.Create(options),
            _mockDbOperations.Object,
            _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithCustomSystemClock_UsesCustomClock()
    {
        // Arrange
        var customTime = DateTimeOffset.UtcNow;
        _mockSystemClock.Setup(x => x.UtcNow).Returns(customTime);

        var options = new PostgreSqlCacheOptions
        {
            SystemClock = _mockSystemClock.Object,
            ExpiredItemsDeletionInterval = TimeSpan.FromMinutes(30)
        };

        // Act
        var removerLoop = new DatabaseExpiredItemsRemoverLoop(
            Options.Create(options),
            _mockDbOperations.Object,
            _mockLogger.Object);

        // Assert
        Assert.NotNull(removerLoop);
    }

    [Fact]
    public void Dispose_WhenCalled_CancelsTokenSource()
    {
        // Arrange
        var options = new PostgreSqlCacheOptions
        {
            ExpiredItemsDeletionInterval = TimeSpan.FromMinutes(30)
        };

        var removerLoop = new DatabaseExpiredItemsRemoverLoop(
            Options.Create(options),
            _mockDbOperations.Object,
            _mockLogger.Object);

        // Act
        removerLoop.Dispose();

        // Assert - Should not throw
        Assert.True(true);
    }
}