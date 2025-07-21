using System;
using System.Threading;
using System.Threading.Tasks;
using Community.Microsoft.Extensions.Caching.PostgreSql;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace CachingTest
{
    public class DatabaseOperationsResilienceTests
    {
        private readonly Mock<ILogger<DatabaseOperations>> _mockLogger;
        private readonly PostgreSqlCacheOptions _options;

        public DatabaseOperationsResilienceTests()
        {
            _mockLogger = new Mock<ILogger<DatabaseOperations>>();
            _options = new PostgreSqlCacheOptions
            {
                SchemaName = "test_schema",
                TableName = "test_table",
                ConnectionString = "Host=localhost;Database=test;Username=test;Password=test",
                EnableResiliencePatterns = true,
                MaxRetryAttempts = 2,
                RetryDelay = TimeSpan.FromMilliseconds(10),
                CircuitBreakerFailureThreshold = 3,
                CircuitBreakerDurationOfBreak = TimeSpan.FromMilliseconds(100),
                OperationTimeout = TimeSpan.FromMilliseconds(10),
                ConnectionFailureLogLevel = LogLevel.Warning,
                EnableResilienceLogging = true,
                CreateInfrastructure = false // Disable infrastructure creation to avoid connection issues in tests
            };
        }

        [Fact]
        public void Constructor_WithResilienceEnabled_ShouldInitializePolicies()
        {
            // Act
            var operations = new DatabaseOperations(Options.Create(_options), _mockLogger.Object);

            // Assert - Should not throw and object should be created successfully
            Assert.NotNull(operations);
        }

        [Fact]
        public void Constructor_WithResilienceDisabled_ShouldNotInitializePolicies()
        {
            // Arrange
            _options.EnableResiliencePatterns = false;

            // Act
            var operations = new DatabaseOperations(Options.Create(_options), _mockLogger.Object);

            // Assert - Should not throw and object should be created successfully
            Assert.NotNull(operations);
        }

        [Fact]
        public void GetCacheItem_WithInvalidConnectionString_ShouldReturnNull()
        {
            // Arrange
            _options.ConnectionString = "Host=invalid-host;Database=invalid-db;Username=invalid-user;Password=invalid-password";
            var operations = new DatabaseOperations(Options.Create(_options), _mockLogger.Object);

            // Act
            var result = operations.GetCacheItem("test-key");

            // Assert
            Assert.Null(result); // Should return null for cache miss behavior
        }

        [Fact]
        public async Task GetCacheItemAsync_WithInvalidConnectionString_ShouldReturnNull()
        {
            // Arrange
            _options.ConnectionString = "Host=invalid-host;Database=invalid-db;Username=invalid-user;Password=invalid-password";
            var operations = new DatabaseOperations(Options.Create(_options), _mockLogger.Object);

            // Act
            var result = await operations.GetCacheItemAsync("test-key", CancellationToken.None);

            // Assert
            Assert.Null(result); // Should return null for cache miss behavior
        }

        [Fact]
        public void SetCacheItem_WithInvalidConnectionString_ShouldSilentlyFail()
        {
            // Arrange
            _options.ConnectionString = "Host=invalid-host;Database=invalid-db;Username=invalid-user;Password=invalid-password";
            var operations = new DatabaseOperations(Options.Create(_options), _mockLogger.Object);
            var options = new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(10)
            };

            // Act & Assert - Should not throw
            operations.SetCacheItem("test-key", new byte[] { 1, 2, 3 }, options);
        }

        [Fact]
        public async Task SetCacheItemAsync_WithInvalidConnectionString_ShouldSilentlyFail()
        {
            // Arrange
            _options.ConnectionString = "Host=invalid-host;Database=invalid-db;Username=invalid-user;Password=invalid-password";
            var operations = new DatabaseOperations(Options.Create(_options), _mockLogger.Object);
            var options = new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(10)
            };

            // Act & Assert - Should not throw
            await operations.SetCacheItemAsync("test-key", new byte[] { 1, 2, 3 }, options, CancellationToken.None);
        }

        [Fact]
        public void DeleteCacheItem_WithInvalidConnectionString_ShouldSilentlyFail()
        {
            // Arrange
            _options.ConnectionString = "Host=invalid-host;Database=invalid-db;Username=invalid-user;Password=invalid-password";
            var operations = new DatabaseOperations(Options.Create(_options), _mockLogger.Object);

            // Act & Assert - Should not throw
            operations.DeleteCacheItem("test-key");
        }

        [Fact]
        public async Task DeleteCacheItemAsync_WithInvalidConnectionString_ShouldSilentlyFail()
        {
            // Arrange
            _options.ConnectionString = "Host=invalid-host;Database=invalid-db;Username=invalid-user;Password=invalid-password";
            var operations = new DatabaseOperations(Options.Create(_options), _mockLogger.Object);

            // Act & Assert - Should not throw
            await operations.DeleteCacheItemAsync("test-key", CancellationToken.None);
        }

        [Fact]
        public void RefreshCacheItem_WithInvalidConnectionString_ShouldSilentlyFail()
        {
            // Arrange
            _options.ConnectionString = "Host=invalid-host;Database=invalid-db;Username=invalid-user;Password=invalid-password";
            var operations = new DatabaseOperations(Options.Create(_options), _mockLogger.Object);

            // Act & Assert - Should not throw
            operations.RefreshCacheItem("test-key");
        }

        [Fact]
        public async Task RefreshCacheItemAsync_WithInvalidConnectionString_ShouldSilentlyFail()
        {
            // Arrange
            _options.ConnectionString = "Host=invalid-host;Database=invalid-db;Username=invalid-user;Password=invalid-password";
            var operations = new DatabaseOperations(Options.Create(_options), _mockLogger.Object);

            // Act & Assert - Should not throw
            await operations.RefreshCacheItemAsync("test-key", CancellationToken.None);
        }

        [Fact]
        public async Task DeleteExpiredCacheItemsAsync_WithInvalidConnectionString_ShouldSilentlyFail()
        {
            // Arrange
            _options.ConnectionString = "Host=invalid-host;Database=invalid-db;Username=invalid-user;Password=invalid-password";
            var operations = new DatabaseOperations(Options.Create(_options), _mockLogger.Object);

            // Act & Assert - Should not throw
            await operations.DeleteExpiredCacheItemsAsync(CancellationToken.None);
        }

        [Fact]
        public void GetCacheItem_WithResilienceDisabled_ShouldThrowException()
        {
            // Arrange
            _options.EnableResiliencePatterns = false;
            _options.ConnectionString = "Host=invalid-host;Database=invalid-db;Username=invalid-user;Password=invalid-password";
            var operations = new DatabaseOperations(Options.Create(_options), _mockLogger.Object);

            // Act & Assert - Should throw since resilience is disabled
            Assert.ThrowsAny<Exception>(() => operations.GetCacheItem("test-key"));
        }

        [Fact]
        public async Task GetCacheItemAsync_WithResilienceDisabled_ShouldThrowException()
        {
            // Arrange
            _options.EnableResiliencePatterns = false;
            _options.ConnectionString = "Host=invalid-host;Database=invalid-db;Username=invalid-user;Password=invalid-password";
            var operations = new DatabaseOperations(Options.Create(_options), _mockLogger.Object);

            // Act & Assert - Should throw since resilience is disabled
            await Assert.ThrowsAnyAsync<Exception>(() => operations.GetCacheItemAsync("test-key", CancellationToken.None));
        }

        [Fact]
        public void SetCacheItem_WithResilienceDisabled_ShouldThrowException()
        {
            // Arrange
            _options.EnableResiliencePatterns = false;
            _options.ConnectionString = "Host=invalid-host;Database=invalid-db;Username=invalid-user;Password=invalid-password";
            var operations = new DatabaseOperations(Options.Create(_options), _mockLogger.Object);
            var options = new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(10)
            };

            // Act & Assert - Should throw since resilience is disabled
            Assert.ThrowsAny<Exception>(() => operations.SetCacheItem("test-key", new byte[] { 1, 2, 3 }, options));
        }

        [Fact]
        public async Task SetCacheItemAsync_WithResilienceDisabled_ShouldThrowException()
        {
            // Arrange
            _options.EnableResiliencePatterns = false;
            _options.ConnectionString = "Host=invalid-host;Database=invalid-db;Username=invalid-user;Password=invalid-password";
            var operations = new DatabaseOperations(Options.Create(_options), _mockLogger.Object);
            var options = new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(10)
            };

            // Act & Assert - Should throw since resilience is disabled
            await Assert.ThrowsAnyAsync<Exception>(() => operations.SetCacheItemAsync("test-key", new byte[] { 1, 2, 3 }, options, CancellationToken.None));
        }

        [Fact]
        public void ReadOnlyMode_ShouldSkipOperations()
        {
            // Arrange
            _options.ReadOnlyMode = true;
            var operations = new DatabaseOperations(Options.Create(_options), _mockLogger.Object);

            // Act & Assert - Should not throw and should log debug message
            operations.SetCacheItem("test-key", new byte[] { 1, 2, 3 }, new DistributedCacheEntryOptions());
            operations.DeleteCacheItem("test-key");

            _mockLogger.Verify(
                x => x.Log(
                    It.Is<LogLevel>(l => l == LogLevel.Debug),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("skipped due to ReadOnlyMode")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public void ResilienceOptions_ShouldLogConnectionFailures()
        {
            // Arrange
            _options.ConnectionString = "Host=invalid-host;Database=invalid-db;Username=invalid-user;Password=invalid-password";
            _options.ConnectionFailureLogLevel = LogLevel.Error;
            var operations = new DatabaseOperations(Options.Create(_options), _mockLogger.Object);

            // Act
            operations.GetCacheItem("test-key");

            // Assert - Should log connection failure at specified level
            _mockLogger.Verify(
                x => x.Log(
                    It.Is<LogLevel>(l => l == LogLevel.Error),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Database connection failed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }
    }
}