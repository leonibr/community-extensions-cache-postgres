using Microsoft.Extensions.Logging;
using Polly;

namespace Community.Microsoft.Extensions.Caching.PostgreSql;

/// <summary>
/// Factory interface for creating Polly resilience policies.
/// </summary>
public interface IPolicyFactory
{
    /// <summary>
    /// Creates a retry policy for transient database failures.
    /// </summary>
    /// <param name="options">The cache options containing retry configuration.</param>
    /// <returns>An async retry policy.</returns>
    IAsyncPolicy CreateRetryPolicy(PostgreSqlCacheOptions options);

    /// <summary>
    /// Creates a circuit breaker policy to prevent cascading failures.
    /// </summary>
    /// <param name="options">The cache options containing circuit breaker configuration.</param>
    /// <returns>An async circuit breaker policy.</returns>
    IAsyncPolicy CreateCircuitBreakerPolicy(PostgreSqlCacheOptions options);

    /// <summary>
    /// Creates a timeout policy for database operations.
    /// </summary>
    /// <param name="options">The cache options containing timeout configuration.</param>
    /// <returns>An async timeout policy.</returns>
    IAsyncPolicy CreateTimeoutPolicy(PostgreSqlCacheOptions options);

    /// <summary>
    /// Creates a combined resilience policy that wraps timeout, circuit breaker, and retry policies.
    /// </summary>
    /// <param name="options">The cache options containing resilience configuration.</param>
    /// <returns>An async resilience policy.</returns>
    IAsyncPolicy CreateResiliencePolicy(PostgreSqlCacheOptions options);
}