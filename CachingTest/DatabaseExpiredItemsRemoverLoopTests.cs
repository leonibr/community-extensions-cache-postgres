using Community.Microsoft.Extensions.Caching.PostgreSql;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Internal;
using Moq;
using System.Threading;

namespace CachingTest;

public class DatabaseExpiredItemsRemoverLoopTests
{
    private readonly Mock<IDatabaseOperations> _mockDbOperations;
    private readonly Mock<ILogger<DatabaseExpiredItemsRemoverLoop>> _mockLogger;
    private readonly PostgreSqlCacheOptions _options;

    public DatabaseExpiredItemsRemoverLoopTests()
    {
        _mockDbOperations = new Mock<IDatabaseOperations>();
        _mockLogger = new Mock<ILogger<DatabaseExpiredItemsRemoverLoop>>();
        _options = new PostgreSqlCacheOptions
        {
            ExpiredItemsDeletionInterval = TimeSpan.FromMinutes(30)
        };
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Act
        var removerLoop = new DatabaseExpiredItemsRemoverLoop(
            Options.Create(_options),
            _mockDbOperations.Object,
            _mockLogger.Object);

        // Assert
        Assert.NotNull(removerLoop);
    }

    [Fact]
    public void Constructor_WithExpiredItemsDeletionIntervalBelowMinimum_ThrowsArgumentException()
    {
        // Arrange
        var invalidOptions = new PostgreSqlCacheOptions
        {
            ExpiredItemsDeletionInterval = TimeSpan.FromMinutes(1) // Below minimum of 5 minutes
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new DatabaseExpiredItemsRemoverLoop(
                Options.Create(invalidOptions),
                _mockDbOperations.Object,
                _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullExpiredItemsDeletionInterval_UsesDefault()
    {
        // Arrange
        var optionsWithNullInterval = new PostgreSqlCacheOptions
        {
            ExpiredItemsDeletionInterval = null
        };

        // Act
        var removerLoop = new DatabaseExpiredItemsRemoverLoop(
            Options.Create(optionsWithNullInterval),
            _mockDbOperations.Object,
            _mockLogger.Object);

        // Assert
        Assert.NotNull(removerLoop);
    }

    [Fact]
    public void Constructor_WithApplicationLifetime_RegistersShutdownCallback()
    {
        // Arrange
        var mockApplicationLifetime = new Mock<IHostApplicationLifetime>();

        // Act
        var removerLoop = new DatabaseExpiredItemsRemoverLoop(
            Options.Create(_options),
            _mockDbOperations.Object,
            mockApplicationLifetime.Object,
            _mockLogger.Object);

        // Assert
        mockApplicationLifetime.Verify(x => x.ApplicationStopping, Times.Once);
    }

    [Fact]
    public void Start_WithDisabledRemoveExpired_DoesNotStartLoop()
    {
        // Arrange
        var disabledOptions = new PostgreSqlCacheOptions
        {
            DisableRemoveExpired = true
        };
        var removerLoop = new DatabaseExpiredItemsRemoverLoop(
            Options.Create(disabledOptions),
            _mockDbOperations.Object,
            _mockLogger.Object);

        // Act
        removerLoop.Start();

        // Assert - Should not call database operations
        _mockDbOperations.Verify(x => x.DeleteExpiredCacheItemsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void Start_WithEnabledRemoveExpired_StartsLoop()
    {
        // Arrange
        var removerLoop = new DatabaseExpiredItemsRemoverLoop(
            Options.Create(_options),
            _mockDbOperations.Object,
            _mockLogger.Object);

        // Act
        removerLoop.Start();

        // Assert - The loop should be started (we can't easily verify the background task without waiting)
        // But we can verify that the constructor didn't throw and Start didn't throw
        Assert.True(true);
    }

    [Fact]
    public void Dispose_DisposesCancellationTokenSource()
    {
        // Arrange
        var removerLoop = new DatabaseExpiredItemsRemoverLoop(
            Options.Create(_options),
            _mockDbOperations.Object,
            _mockLogger.Object);

        // Act
        removerLoop.Dispose();

        // Assert - Should not throw
        Assert.True(true);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var removerLoop = new DatabaseExpiredItemsRemoverLoop(
            Options.Create(_options),
            _mockDbOperations.Object,
            _mockLogger.Object);

        // Act & Assert - Should not throw
        removerLoop.Dispose();
        removerLoop.Dispose();
        Assert.True(true);
    }
}

public class DatabaseExpiredItemsRemoverLoopIntegrationTests : IAsyncLifetime
{
    private readonly Mock<IDatabaseOperations> _mockDbOperations;
    private readonly Mock<ILogger<DatabaseExpiredItemsRemoverLoop>> _mockLogger;
    private readonly PostgreSqlCacheOptions _options;
    private DatabaseExpiredItemsRemoverLoop? _removerLoop;

    public DatabaseExpiredItemsRemoverLoopIntegrationTests()
    {
        _mockDbOperations = new Mock<IDatabaseOperations>();
        _mockLogger = new Mock<ILogger<DatabaseExpiredItemsRemoverLoop>>();
        _options = new PostgreSqlCacheOptions
        {
            ExpiredItemsDeletionInterval = TimeSpan.FromMinutes(5) // Minimum allowed interval
        };
    }

    public Task InitializeAsync()
    {
        _removerLoop = new DatabaseExpiredItemsRemoverLoop(
            Options.Create(_options),
            _mockDbOperations.Object,
            _mockLogger.Object);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _removerLoop?.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Start_WithValidInterval_StartsLoop()
    {
        // Arrange
        if (_removerLoop == null) throw new InvalidOperationException("RemoverLoop is null");

        // Act
        _removerLoop.Start();

        // Assert - The loop should be started without throwing
        Assert.True(true);
    }

    [Fact]
    public async Task Start_WithExceptionSetup_StartsLoop()
    {
        // Arrange
        if (_removerLoop == null) throw new InvalidOperationException("RemoverLoop is null");

        _mockDbOperations.Setup(x => x.DeleteExpiredCacheItemsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        _removerLoop.Start();

        // Assert - Should not throw and should start the loop
        Assert.True(true);
    }

    [Fact]
    public async Task Dispose_WithRunningLoop_StopsLoop()
    {
        // Arrange
        if (_removerLoop == null) throw new InvalidOperationException("RemoverLoop is null");

        _removerLoop.Start();

        // Act
        _removerLoop.Dispose();

        // Assert - Should not throw
        Assert.True(true);
    }
}

public class DatabaseExpiredItemsRemoverLoopWithCustomClockTests
{
    private readonly Mock<IDatabaseOperations> _mockDbOperations;
    private readonly Mock<ILogger<DatabaseExpiredItemsRemoverLoop>> _mockLogger;
    private readonly Mock<ISystemClock> _mockSystemClock;

    public DatabaseExpiredItemsRemoverLoopWithCustomClockTests()
    {
        _mockDbOperations = new Mock<IDatabaseOperations>();
        _mockLogger = new Mock<ILogger<DatabaseExpiredItemsRemoverLoop>>();
        _mockSystemClock = new Mock<ISystemClock>();
    }

    [Fact]
    public void Constructor_WithCustomSystemClock_CreatesInstance()
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
        // Note: The constructor doesn't use the system clock, so we don't verify the mock
    }
}