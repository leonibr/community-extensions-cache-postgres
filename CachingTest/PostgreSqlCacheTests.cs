using Community.Microsoft.Extensions.Caching.PostgreSql;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Text;

namespace CachingTest;

public class PostgreSqlCacheTests
{
    private readonly Mock<IDatabaseOperations> _mockDbOperations;
    private readonly Mock<IDatabaseExpiredItemsRemoverLoop> _mockRemoverLoop;
    private readonly PostgreSqlCacheOptions _options;
    private readonly PostgreSqlCache _cache;

    public PostgreSqlCacheTests()
    {
        _mockDbOperations = new Mock<IDatabaseOperations>();
        _mockRemoverLoop = new Mock<IDatabaseExpiredItemsRemoverLoop>();
        _options = new PostgreSqlCacheOptions
        {
            DefaultSlidingExpiration = TimeSpan.FromMinutes(20)
        };
        _cache = new PostgreSqlCache(Options.Create(_options), _mockDbOperations.Object, _mockRemoverLoop.Object);
    }

    [Fact]
    public void Constructor_WithNullDatabaseOperations_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new PostgreSqlCache(Options.Create(_options), null!, _mockRemoverLoop.Object));
    }

    [Fact]
    public void Constructor_WithNullRemoverLoop_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new PostgreSqlCache(Options.Create(_options), _mockDbOperations.Object, null!));
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new PostgreSqlCache(null!, _mockDbOperations.Object, _mockRemoverLoop.Object));
    }

    [Fact]
    public void Constructor_WithInvalidDefaultSlidingExpiration_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var invalidOptions = new PostgreSqlCacheOptions
        {
            DefaultSlidingExpiration = TimeSpan.Zero
        };

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PostgreSqlCache(Options.Create(invalidOptions), _mockDbOperations.Object, _mockRemoverLoop.Object));
    }

    [Fact]
    public void Constructor_WithValidParameters_StartsRemoverLoop()
    {
        // Assert
        _mockRemoverLoop.Verify(x => x.Start(), Times.Once);
    }

    [Fact]
    public void Get_WithNullKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _cache.Get(null!));
    }

    [Fact]
    public void Get_WithValidKey_CallsDatabaseOperations()
    {
        // Arrange
        var key = "test-key";
        var expectedValue = new byte[] { 1, 2, 3 };
        _mockDbOperations.Setup(x => x.GetCacheItem(key)).Returns(expectedValue);

        // Act
        var result = _cache.Get(key);

        // Assert
        Assert.Equal(expectedValue, result);
        _mockDbOperations.Verify(x => x.GetCacheItem(key), Times.Once);
    }

    [Fact]
    public async Task GetAsync_WithNullKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _cache.GetAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetAsync_WithValidKey_CallsDatabaseOperations()
    {
        // Arrange
        var key = "test-key";
        var expectedValue = new byte[] { 1, 2, 3 };
        _mockDbOperations.Setup(x => x.GetCacheItemAsync(key, CancellationToken.None))
            .ReturnsAsync(expectedValue);

        // Act
        var result = await _cache.GetAsync(key, CancellationToken.None);

        // Assert
        Assert.Equal(expectedValue, result);
        _mockDbOperations.Verify(x => x.GetCacheItemAsync(key, CancellationToken.None), Times.Once);
    }

    [Fact]
    public void Refresh_WithNullKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _cache.Refresh(null!));
    }

    [Fact]
    public void Refresh_WithValidKey_CallsDatabaseOperations()
    {
        // Arrange
        var key = "test-key";

        // Act
        _cache.Refresh(key);

        // Assert
        _mockDbOperations.Verify(x => x.RefreshCacheItem(key), Times.Once);
    }

    [Fact]
    public async Task RefreshAsync_WithNullKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _cache.RefreshAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task RefreshAsync_WithValidKey_CallsDatabaseOperations()
    {
        // Arrange
        var key = "test-key";

        // Act
        await _cache.RefreshAsync(key, CancellationToken.None);

        // Assert
        _mockDbOperations.Verify(x => x.RefreshCacheItemAsync(key, CancellationToken.None), Times.Once);
    }

    [Fact]
    public void Remove_WithNullKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _cache.Remove(null!));
    }

    [Fact]
    public void Remove_WithValidKey_CallsDatabaseOperations()
    {
        // Arrange
        var key = "test-key";

        // Act
        _cache.Remove(key);

        // Assert
        _mockDbOperations.Verify(x => x.DeleteCacheItem(key), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_WithNullKey_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _cache.RemoveAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task RemoveAsync_WithValidKey_CallsDatabaseOperations()
    {
        // Arrange
        var key = "test-key";

        // Act
        await _cache.RemoveAsync(key, CancellationToken.None);

        // Assert
        _mockDbOperations.Verify(x => x.DeleteCacheItemAsync(key, CancellationToken.None), Times.Once);
    }

    [Fact]
    public void Set_WithNullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var value = new byte[] { 1, 2, 3 };
        var options = new DistributedCacheEntryOptions();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _cache.Set(null!, value, options));
    }

    [Fact]
    public void Set_WithNullValue_ThrowsArgumentNullException()
    {
        // Arrange
        var key = "test-key";
        var options = new DistributedCacheEntryOptions();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _cache.Set(key, null!, options));
    }

    [Fact]
    public void Set_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var key = "test-key";
        var value = new byte[] { 1, 2, 3 };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _cache.Set(key, value, null!));
    }

    [Fact]
    public void Set_WithValidParameters_CallsDatabaseOperations()
    {
        // Arrange
        var key = "test-key";
        var value = new byte[] { 1, 2, 3 };
        var options = new DistributedCacheEntryOptions();

        // Act
        _cache.Set(key, value, options);

        // Assert
        _mockDbOperations.Verify(x => x.SetCacheItem(key, value, It.IsAny<DistributedCacheEntryOptions>()), Times.Once);
    }

    [Fact]
    public void Set_WithNoExpirationOptions_AppliesDefaultSlidingExpiration()
    {
        // Arrange
        var key = "test-key";
        var value = new byte[] { 1, 2, 3 };
        var options = new DistributedCacheEntryOptions();

        // Act
        _cache.Set(key, value, options);

        // Assert
        _mockDbOperations.Verify(x => x.SetCacheItem(key, value, It.Is<DistributedCacheEntryOptions>(o =>
            o.SlidingExpiration == TimeSpan.FromMinutes(20))), Times.Once);
    }

    [Fact]
    public async Task SetAsync_WithNullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var value = new byte[] { 1, 2, 3 };
        var options = new DistributedCacheEntryOptions();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _cache.SetAsync(null!, value, options, CancellationToken.None));
    }

    [Fact]
    public async Task SetAsync_WithNullValue_ThrowsArgumentNullException()
    {
        // Arrange
        var key = "test-key";
        var options = new DistributedCacheEntryOptions();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _cache.SetAsync(key, null!, options, CancellationToken.None));
    }

    [Fact]
    public async Task SetAsync_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var key = "test-key";
        var value = new byte[] { 1, 2, 3 };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _cache.SetAsync(key, value, null!, CancellationToken.None));
    }

    [Fact]
    public async Task SetAsync_WithValidParameters_CallsDatabaseOperations()
    {
        // Arrange
        var key = "test-key";
        var value = new byte[] { 1, 2, 3 };
        var options = new DistributedCacheEntryOptions();

        // Act
        await _cache.SetAsync(key, value, options, CancellationToken.None);

        // Assert
        _mockDbOperations.Verify(x => x.SetCacheItemAsync(key, value, It.IsAny<DistributedCacheEntryOptions>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task SetAsync_WithNoExpirationOptions_AppliesDefaultSlidingExpiration()
    {
        // Arrange
        var key = "test-key";
        var value = new byte[] { 1, 2, 3 };
        var options = new DistributedCacheEntryOptions();

        // Act
        await _cache.SetAsync(key, value, options, CancellationToken.None);

        // Assert
        _mockDbOperations.Verify(x => x.SetCacheItemAsync(key, value, It.Is<DistributedCacheEntryOptions>(o =>
            o.SlidingExpiration == TimeSpan.FromMinutes(20)), CancellationToken.None), Times.Once);
    }

    [Fact]
    public void Set_WithExistingExpirationOptions_DoesNotOverride()
    {
        // Arrange
        var key = "test-key";
        var value = new byte[] { 1, 2, 3 };
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };

        // Act
        _cache.Set(key, value, options);

        // Assert
        _mockDbOperations.Verify(x => x.SetCacheItem(key, value, It.Is<DistributedCacheEntryOptions>(o =>
            o.AbsoluteExpirationRelativeToNow == TimeSpan.FromMinutes(5))), Times.Once);
    }
}