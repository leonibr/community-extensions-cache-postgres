using System;
using System.Threading.Tasks;
using Community.Microsoft.Extensions.Caching.PostgreSql;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;
using System.Net.Sockets;
using System.Reflection;
using Xunit;

namespace CachingTest
{
    public class PolicyFactoryTests
    {
        private readonly PolicyFactory _policyFactory;
        private readonly PostgreSqlCacheOptions _options;

        public PolicyFactoryTests()
        {
            var mockLogger = new Mock<ILogger>();
            _policyFactory = new PolicyFactory(mockLogger.Object);

            _options = new PostgreSqlCacheOptions
            {
                EnableResiliencePatterns = true,
                MaxRetryAttempts = 3,
                RetryDelay = TimeSpan.FromMilliseconds(10), // Use shorter delays for tests
                CircuitBreakerFailureThreshold = 2,
                CircuitBreakerDurationOfBreak = TimeSpan.FromMilliseconds(50), // Use shorter duration for tests
                OperationTimeout = TimeSpan.FromMilliseconds(100),
                EnableResilienceLogging = true
            };
        }

        [Fact]
        public void CreateRetryPolicy_ShouldReturnValidPolicy()
        {
            // Act
            var policy = _policyFactory.CreateRetryPolicy(_options);

            // Assert
            Assert.NotNull(policy);
        }

        [Fact]
        public void CreateCircuitBreakerPolicy_ShouldReturnValidPolicy()
        {
            // Act
            var policy = _policyFactory.CreateCircuitBreakerPolicy(_options);

            // Assert
            Assert.NotNull(policy);
        }

        [Fact]
        public void CreateTimeoutPolicy_ShouldReturnValidPolicy()
        {
            // Act
            var policy = _policyFactory.CreateTimeoutPolicy(_options);

            // Assert
            Assert.NotNull(policy);
        }

        [Fact]
        public void CreateResiliencePolicy_ShouldReturnValidPolicy()
        {
            // Act
            var policy = _policyFactory.CreateResiliencePolicy(_options);

            // Assert
            Assert.NotNull(policy);
        }

        [Fact]
        public async Task RetryPolicy_ShouldExecuteSuccessfulOperation()
        {
            // Arrange
            var policy = _policyFactory.CreateRetryPolicy(_options);
            var executed = false;

            // Act
            await policy.ExecuteAsync(async () =>
            {
                executed = true;
                await Task.CompletedTask;
            });

            // Assert
            Assert.True(executed);
        }

        [Fact]
        public async Task CircuitBreakerPolicy_ShouldExecuteSuccessfulOperation()
        {
            // Arrange
            var policy = _policyFactory.CreateCircuitBreakerPolicy(_options);
            var executed = false;

            // Act
            await policy.ExecuteAsync(async () =>
            {
                executed = true;
                await Task.CompletedTask;
            });

            // Assert
            Assert.True(executed);
        }

        [Fact]
        public async Task TimeoutPolicy_ShouldExecuteQuickOperation()
        {
            // Arrange
            var policy = _policyFactory.CreateTimeoutPolicy(_options);
            var executed = false;

            // Act
            await policy.ExecuteAsync(async () =>
            {
                executed = true;
                await Task.Delay(10); // Short delay
            });

            // Assert
            Assert.True(executed);
        }

        [Fact]
        public async Task TimeoutPolicy_ShouldTimeoutOnLongOperation()
        {
            // Arrange
            var shortTimeoutOptions = new PostgreSqlCacheOptions
            {
                OperationTimeout = TimeSpan.FromMilliseconds(20)
            };
            var policy = _policyFactory.CreateTimeoutPolicy(shortTimeoutOptions);

            // Act & Assert
            await Assert.ThrowsAsync<TimeoutRejectedException>(async () =>
            {
                await policy.ExecuteAsync(async () =>
                {
                    await Task.Delay(100); // Longer than timeout
                });
            });
        }

        [Fact]
        public async Task ResiliencePolicy_ShouldExecuteSuccessfulOperation()
        {
            // Arrange
            var policy = _policyFactory.CreateResiliencePolicy(_options);
            var executed = false;

            // Act
            await policy.ExecuteAsync(async () =>
            {
                executed = true;
                await Task.CompletedTask;
            });

            // Assert
            Assert.True(executed);
        }

        [Fact]
        public async Task RetryPolicy_ShouldRetryOnSocketException()
        {
            // Arrange
            var policy = _policyFactory.CreateRetryPolicy(_options);
            var attemptCount = 0;
            var maxAttempts = 4; // Will attempt initial + 3 retries (MaxRetryAttempts = 3)

            // Act & Assert - SocketException should be retried
            await Assert.ThrowsAsync<SocketException>(async () =>
            {
                await policy.ExecuteAsync(async () =>
                {
                    attemptCount++;
                    if (attemptCount <= maxAttempts)
                    {
                        throw new SocketException();
                    }
                    await Task.CompletedTask;
                });
            });

            // Should have attempted the operation multiple times
            Assert.Equal(maxAttempts, attemptCount);
        }

        [Fact]
        public async Task RetryPolicy_ShouldEventuallySucceed()
        {
            // Arrange
            var policy = _policyFactory.CreateRetryPolicy(_options);
            var attemptCount = 0;
            var successAfterAttempts = 2;

            // Act - Use SocketException which should be retried
            await policy.ExecuteAsync(async () =>
            {
                attemptCount++;
                if (attemptCount < successAfterAttempts)
                {
                    throw new SocketException();
                }
                await Task.CompletedTask;
            });

            // Assert
            Assert.Equal(successAfterAttempts, attemptCount);
        }

        [Fact]
        public void PolicyFactory_ExceptionClassification_ShouldHaveCorrectMethods()
        {
            // Test that the PolicyFactory has the expected private static methods for exception classification
            var policyFactoryType = typeof(PolicyFactory);

            var isTransientMethod = policyFactoryType.GetMethod("IsTransientException", BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(PostgresException) }, null);
            var isPermanentMethod = policyFactoryType.GetMethod("IsPermanentException", BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(PostgresException) }, null);

            Assert.NotNull(isTransientMethod);
            Assert.NotNull(isPermanentMethod);
        }

        [Fact]
        public void PolicyFactory_ExceptionClassification_ShouldWorkWithTestException()
        {
            // Test that exception classification methods work with a test PostgresException
            var policyFactoryType = typeof(PolicyFactory);
            var isTransientMethod = policyFactoryType.GetMethod("IsTransientException", BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(PostgresException) }, null);
            var isPermanentMethod = policyFactoryType.GetMethod("IsPermanentException", BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(PostgresException) }, null);

            Assert.NotNull(isTransientMethod);
            Assert.NotNull(isPermanentMethod);

            // Create a test PostgresException with a known transient SQL state
            var transientException = new PostgresException("Connection failed", "ERROR", "ERROR", "08001");
            var permanentException = new PostgresException("Authentication failed", "ERROR", "ERROR", "28P01");

#pragma warning disable CS8605 // Unboxing a possibly null value.
            var isTransient = (bool)isTransientMethod.Invoke(null, new object[] { transientException });
            var isPermanent = (bool)isPermanentMethod.Invoke(null, new object[] { permanentException });
#pragma warning restore CS8605 // Unboxing a possibly null value.

            // Debug output to see what's happening
            var transientSqlState = transientException.SqlState;
            var permanentSqlState = permanentException.SqlState;

            // Check that classification works correctly
            Assert.True(isTransient, $"Transient exception with SQL state '{transientSqlState}' should return true");
            Assert.True(isPermanent, $"Permanent exception with SQL state '{permanentSqlState}' should return true");
        }

        [Fact]
        public async Task ResiliencePolicy_ShouldCombineAllPolicies()
        {
            // Arrange
            var policy = _policyFactory.CreateResiliencePolicy(_options);
            var attemptCount = 0;

            // Act - Test that it works with successful operations
            await policy.ExecuteAsync(async () =>
            {
                attemptCount++;
                await Task.CompletedTask;
            });

            // Assert
            Assert.Equal(1, attemptCount);
        }

        [Fact]
        public void PolicyFactoryOptions_ShouldRespectConfiguration()
        {
            // Test that the PolicyFactory respects the configuration options
            var customOptions = new PostgreSqlCacheOptions
            {
                MaxRetryAttempts = 5,
                RetryDelay = TimeSpan.FromSeconds(2),
                CircuitBreakerFailureThreshold = 10,
                CircuitBreakerDurationOfBreak = TimeSpan.FromMinutes(1),
                OperationTimeout = TimeSpan.FromSeconds(30)
            };

            // Act - Should not throw when creating policies with valid options
            var retryPolicy = _policyFactory.CreateRetryPolicy(customOptions);
            var circuitBreakerPolicy = _policyFactory.CreateCircuitBreakerPolicy(customOptions);
            var timeoutPolicy = _policyFactory.CreateTimeoutPolicy(customOptions);
            var resiliencePolicy = _policyFactory.CreateResiliencePolicy(customOptions);

            // Assert
            Assert.NotNull(retryPolicy);
            Assert.NotNull(circuitBreakerPolicy);
            Assert.NotNull(timeoutPolicy);
            Assert.NotNull(resiliencePolicy);
        }
    }
}