using Community.Microsoft.Extensions.Caching.PostgreSql;
using Microsoft.Extensions.Logging;

namespace CachingTest
{
    public class PostgreSqlCacheOptionsResilienceTests
    {
        [Fact]
        public void DefaultValues_ShouldBeSensible()
        {
            // Arrange & Act
            var options = new PostgreSqlCacheOptions();

            // Assert
            Assert.True(options.EnableResiliencePatterns);
            Assert.Equal(3, options.MaxRetryAttempts);
            Assert.Equal(TimeSpan.FromSeconds(1), options.RetryDelay);
            Assert.True(options.EnableCircuitBreaker);
            Assert.Equal(5, options.CircuitBreakerFailureThreshold);
            Assert.Equal(TimeSpan.FromMinutes(1), options.CircuitBreakerDurationOfBreak);
            Assert.Equal(LogLevel.Warning, options.ConnectionFailureLogLevel);
            Assert.Equal(TimeSpan.FromSeconds(30), options.OperationTimeout);
            Assert.False(options.EnableResilienceLogging);
        }

        [Fact]
        public void ValidateResilienceConfiguration_WithValidValues_ShouldNotThrow()
        {
            // Arrange
            var options = new PostgreSqlCacheOptions
            {
                MaxRetryAttempts = 5,
                RetryDelay = TimeSpan.FromSeconds(2),
                CircuitBreakerFailureThreshold = 10,
                CircuitBreakerDurationOfBreak = TimeSpan.FromMinutes(2),
                OperationTimeout = TimeSpan.FromSeconds(60)
            };

            // Act & Assert - Should not throw
            options.ValidateResilienceConfiguration();
        }

        [Fact]
        public void ValidateResilienceConfiguration_WithNegativeMaxRetryAttempts_ShouldThrowArgumentException()
        {
            // Arrange
            var options = new PostgreSqlCacheOptions
            {
                MaxRetryAttempts = -1
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => options.ValidateResilienceConfiguration());
            Assert.Contains(nameof(PostgreSqlCacheOptions.MaxRetryAttempts), exception.Message);
        }

        [Fact]
        public void ValidateResilienceConfiguration_WithTooHighMaxRetryAttempts_ShouldThrowArgumentException()
        {
            // Arrange
            var options = new PostgreSqlCacheOptions
            {
                MaxRetryAttempts = 11
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => options.ValidateResilienceConfiguration());
            Assert.Contains(nameof(PostgreSqlCacheOptions.MaxRetryAttempts), exception.Message);
        }

        [Fact]
        public void ValidateResilienceConfiguration_WithZeroRetryDelay_ShouldThrowArgumentException()
        {
            // Arrange
            var options = new PostgreSqlCacheOptions
            {
                RetryDelay = TimeSpan.Zero
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => options.ValidateResilienceConfiguration());
            Assert.Contains(nameof(PostgreSqlCacheOptions.RetryDelay), exception.Message);
        }

        [Fact]
        public void ValidateResilienceConfiguration_WithTooHighRetryDelay_ShouldThrowArgumentException()
        {
            // Arrange
            var options = new PostgreSqlCacheOptions
            {
                RetryDelay = TimeSpan.FromMinutes(10)
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => options.ValidateResilienceConfiguration());
            Assert.Contains(nameof(PostgreSqlCacheOptions.RetryDelay), exception.Message);
        }

        [Fact]
        public void ValidateResilienceConfiguration_WithZeroCircuitBreakerFailureThreshold_ShouldThrowArgumentException()
        {
            // Arrange
            var options = new PostgreSqlCacheOptions
            {
                CircuitBreakerFailureThreshold = 0
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => options.ValidateResilienceConfiguration());
            Assert.Contains(nameof(PostgreSqlCacheOptions.CircuitBreakerFailureThreshold), exception.Message);
        }

        [Fact]
        public void ValidateResilienceConfiguration_WithTooHighCircuitBreakerFailureThreshold_ShouldThrowArgumentException()
        {
            // Arrange
            var options = new PostgreSqlCacheOptions
            {
                CircuitBreakerFailureThreshold = 101
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => options.ValidateResilienceConfiguration());
            Assert.Contains(nameof(PostgreSqlCacheOptions.CircuitBreakerFailureThreshold), exception.Message);
        }

        [Fact]
        public void ValidateResilienceConfiguration_WithZeroCircuitBreakerDurationOfBreak_ShouldThrowArgumentException()
        {
            // Arrange
            var options = new PostgreSqlCacheOptions
            {
                CircuitBreakerDurationOfBreak = TimeSpan.Zero
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => options.ValidateResilienceConfiguration());
            Assert.Contains(nameof(PostgreSqlCacheOptions.CircuitBreakerDurationOfBreak), exception.Message);
        }

        [Fact]
        public void ValidateResilienceConfiguration_WithTooHighCircuitBreakerDurationOfBreak_ShouldThrowArgumentException()
        {
            // Arrange
            var options = new PostgreSqlCacheOptions
            {
                CircuitBreakerDurationOfBreak = TimeSpan.FromHours(2)
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => options.ValidateResilienceConfiguration());
            Assert.Contains(nameof(PostgreSqlCacheOptions.CircuitBreakerDurationOfBreak), exception.Message);
        }

        [Fact]
        public void ValidateResilienceConfiguration_WithZeroOperationTimeout_ShouldThrowArgumentException()
        {
            // Arrange
            var options = new PostgreSqlCacheOptions
            {
                OperationTimeout = TimeSpan.Zero
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => options.ValidateResilienceConfiguration());
            Assert.Contains(nameof(PostgreSqlCacheOptions.OperationTimeout), exception.Message);
        }

        [Fact]
        public void ValidateResilienceConfiguration_WithTooHighOperationTimeout_ShouldThrowArgumentException()
        {
            // Arrange
            var options = new PostgreSqlCacheOptions
            {
                OperationTimeout = TimeSpan.FromMinutes(15)
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => options.ValidateResilienceConfiguration());
            Assert.Contains(nameof(PostgreSqlCacheOptions.OperationTimeout), exception.Message);
        }

        [Fact]
        public void ValidateResilienceConfiguration_WithMultipleInvalidValues_ShouldThrowFirstException()
        {
            // Arrange
            var options = new PostgreSqlCacheOptions
            {
                MaxRetryAttempts = -1,
                RetryDelay = TimeSpan.Zero,
                CircuitBreakerFailureThreshold = 0
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => options.ValidateResilienceConfiguration());
            Assert.Contains(nameof(PostgreSqlCacheOptions.MaxRetryAttempts), exception.Message);
        }

        [Fact]
        public void EnableResiliencePatterns_WhenFalse_ShouldNotValidateOtherOptions()
        {
            // Arrange
            var options = new PostgreSqlCacheOptions
            {
                EnableResiliencePatterns = false,
                MaxRetryAttempts = -1, // Invalid value
                RetryDelay = TimeSpan.Zero, // Invalid value
                CircuitBreakerFailureThreshold = 0 // Invalid value
            };

            // Act & Assert - Should not throw because resilience is disabled
            options.ValidateResilienceConfiguration();
        }

        [Fact]
        public void LogLevel_ShouldAcceptAllValidLogLevels()
        {
            // Arrange & Act
            var options = new PostgreSqlCacheOptions();

            // Test all valid log levels
            var validLogLevels = new[]
            {
                LogLevel.Trace,
                LogLevel.Debug,
                LogLevel.Information,
                LogLevel.Warning,
                LogLevel.Error,
                LogLevel.Critical
            };

            foreach (var logLevel in validLogLevels)
            {
                options.ConnectionFailureLogLevel = logLevel;
                // Should not throw
                options.ValidateResilienceConfiguration();
            }
        }

        [Fact]
        public void EnableResilienceLogging_ShouldNotAffectValidation()
        {
            // Arrange
            var options = new PostgreSqlCacheOptions
            {
                EnableResilienceLogging = true
            };

            // Act & Assert - Should not throw
            options.ValidateResilienceConfiguration();
        }

        [Fact]
        public void BackwardCompatibility_WithOldOptions_ShouldWork()
        {
            // Arrange - Simulate old options without resilience properties
            var options = new PostgreSqlCacheOptions
            {
                ConnectionString = "test-connection-string",
                SchemaName = "test_schema",
                TableName = "test_table",
                // No resilience properties set - should use defaults
            };

            // Act & Assert - Should not throw and should have sensible defaults
            Assert.True(options.EnableResiliencePatterns);
            Assert.Equal(3, options.MaxRetryAttempts);
            Assert.Equal(TimeSpan.FromSeconds(1), options.RetryDelay);
            Assert.True(options.EnableCircuitBreaker);
            Assert.Equal(5, options.CircuitBreakerFailureThreshold);
            Assert.Equal(TimeSpan.FromMinutes(1), options.CircuitBreakerDurationOfBreak);
            Assert.Equal(LogLevel.Warning, options.ConnectionFailureLogLevel);
            Assert.Equal(TimeSpan.FromSeconds(30), options.OperationTimeout);
            Assert.False(options.EnableResilienceLogging);

            // Validation should pass with defaults
            options.ValidateResilienceConfiguration();
        }

        [Fact]
        public void ResiliencePatterns_InteractionWithCircuitBreaker_ShouldWorkAsExpected()
        {
            // Test the hierarchical relationship between EnableResiliencePatterns and EnableCircuitBreaker

            // Scenario 1: EnableResiliencePatterns = false, EnableCircuitBreaker = false
            var options1 = new PostgreSqlCacheOptions
            {
                EnableResiliencePatterns = false,
                EnableCircuitBreaker = false
            };
            // Should not validate sub-options when resilience patterns are disabled
            options1.ValidateResilienceConfiguration();

            // Scenario 2: EnableResiliencePatterns = false, EnableCircuitBreaker = true
            var options2 = new PostgreSqlCacheOptions
            {
                EnableResiliencePatterns = false,
                EnableCircuitBreaker = true,  // This will be IGNORED
                CircuitBreakerFailureThreshold = 0  // Invalid value, but should not throw
            };
            // Should not validate sub-options when resilience patterns are disabled
            options2.ValidateResilienceConfiguration();

            // Scenario 3: EnableResiliencePatterns = true, EnableCircuitBreaker = false
            var options3 = new PostgreSqlCacheOptions
            {
                EnableResiliencePatterns = true,
                EnableCircuitBreaker = false,  // Circuit breaker will be excluded from policy chain
                CircuitBreakerFailureThreshold = 5  // Valid value (will be ignored in policy creation)
            };
            // Should validate all options when resilience patterns are enabled
            options3.ValidateResilienceConfiguration();

            // Scenario 4: EnableResiliencePatterns = true, EnableCircuitBreaker = true
            var options4 = new PostgreSqlCacheOptions
            {
                EnableResiliencePatterns = true,
                EnableCircuitBreaker = true,  // Circuit breaker will be included in policy chain
                CircuitBreakerFailureThreshold = 5  // Valid value
            };
            // Should validate all options when resilience patterns are enabled
            options4.ValidateResilienceConfiguration();

            // Scenario 5: EnableResiliencePatterns = true, EnableCircuitBreaker = true with invalid settings
            var options5 = new PostgreSqlCacheOptions
            {
                EnableResiliencePatterns = true,
                EnableCircuitBreaker = true,
                CircuitBreakerFailureThreshold = 0  // Invalid value
            };
            // Should throw when resilience patterns are enabled and circuit breaker settings are invalid
            Assert.Throws<ArgumentException>(() => options5.ValidateResilienceConfiguration());
        }
    }
}