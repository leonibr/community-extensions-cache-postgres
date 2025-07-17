using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;
using Benchmarks.Fixtures;

namespace Benchmarks.UseCases;

/// <summary>
/// Benchmarks for measuring the performance impact of different cache expiration strategies.
/// Tests Set, Get, and Refresh operations with various expiration configurations including:
/// <list type="bullet">
///    <item>No expiration (default sliding expiration)</item>
///    <item>Sliding expiration (30 minutes)</item>
///    <item>Absolute expiration (relative to now)</item>
///    <item>Absolute expiration (fixed time)</item>
///    <item>Combined sliding and absolute expiration</item>
///    <item>Short-term expiration (5 minutes)</item>
/// </list>
/// Measures both async and sync operation performance.
/// </summary>
[MemoryDiagnoser]
[RankColumn]
[MeanColumn, MedianColumn, StdDevColumn, MinColumn, MaxColumn]
public class ExpirationBenchmark : IAsyncDisposable
{
    private PostgreSqlBenchmarkFixture _fixture = null!;
    private IDistributedCache _cache = null!;
    private readonly byte[] _testData = Encoding.UTF8.GetBytes("This is a test cache value for expiration benchmarking purposes.");

    // Different expiration option configurations
    private readonly DistributedCacheEntryOptions _noExpirationOptions = new()
    {
        // No expiration set - uses default sliding expiration
    };

    private readonly DistributedCacheEntryOptions _slidingExpirationOptions = new()
    {
        SlidingExpiration = TimeSpan.FromMinutes(30)
    };

    private readonly DistributedCacheEntryOptions _absoluteExpirationOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60)
    };

    private readonly DistributedCacheEntryOptions _absoluteExpirationFixedOptions = new()
    {
        AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(60)
    };

    private readonly DistributedCacheEntryOptions _bothExpirationOptions = new()
    {
        SlidingExpiration = TimeSpan.FromMinutes(30),
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60)
    };

    private readonly DistributedCacheEntryOptions _shortExpirationOptions = new()
    {
        SlidingExpiration = TimeSpan.FromMinutes(5)
    };

    [GlobalSetup]
    public void GlobalSetup()
    {
        _fixture = new PostgreSqlBenchmarkFixture();
        _cache = _fixture.InitializeAsync().GetAwaiter().GetResult();

        // Pre-populate cache with different expiration strategies
        _cache.Set("no_expiration_key", _testData, _noExpirationOptions);
        _cache.Set("sliding_expiration_key", _testData, _slidingExpirationOptions);
        _cache.Set("absolute_expiration_key", _testData, _absoluteExpirationOptions);
        _cache.Set("both_expiration_key", _testData, _bothExpirationOptions);
        _cache.Set("short_expiration_key", _testData, _shortExpirationOptions);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _fixture.DisposeAsync().GetAwaiter().GetResult();
    }

    [IterationSetup]
    public async Task IterationSetup()
    {
        // Clean up and re-populate cache
        await _fixture.CleanupAsync();

        _cache.Set("no_expiration_key", _testData, _noExpirationOptions);
        _cache.Set("sliding_expiration_key", _testData, _slidingExpirationOptions);
        _cache.Set("absolute_expiration_key", _testData, _absoluteExpirationOptions);
        _cache.Set("both_expiration_key", _testData, _bothExpirationOptions);
        _cache.Set("short_expiration_key", _testData, _shortExpirationOptions);
    }

    [Benchmark(Baseline = true)]
    public async Task SetAsync_NoExpiration()
    {
        var key = $"no_exp_set_{Random.Shared.Next(10000)}";
        await _cache.SetAsync(key, _testData, _noExpirationOptions);
    }

    [Benchmark]
    public async Task<string> SetAsync_SlidingExpiration()
    {
        var key = $"sliding_exp_set_{Random.Shared.Next(10000)}";
        await _cache.SetAsync(key, _testData, _slidingExpirationOptions);
        return nameof(SetAsync_SlidingExpiration);
    }

    [Benchmark]
    public async Task SetAsync_AbsoluteExpiration()
    {
        var key = $"absolute_exp_set_{Random.Shared.Next(10000)}";
        await _cache.SetAsync(key, _testData, _absoluteExpirationOptions);
    }

    [Benchmark]
    public async Task SetAsync_AbsoluteExpirationFixed()
    {
        var key = $"absolute_fixed_exp_set_{Random.Shared.Next(10000)}";
        // Create new options with fresh absolute expiration time
        var freshOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(60)
        };
        await _cache.SetAsync(key, _testData, freshOptions);
    }

    [Benchmark]
    public async Task SetAsync_BothExpirations()
    {
        var key = $"both_exp_set_{Random.Shared.Next(10000)}";
        await _cache.SetAsync(key, _testData, _bothExpirationOptions);
    }

    [Benchmark]
    public async Task SetAsync_ShortExpiration()
    {
        var key = $"short_exp_set_{Random.Shared.Next(10000)}";
        await _cache.SetAsync(key, _testData, _shortExpirationOptions);
    }

    [Benchmark]
    public async Task GetAsync_NoExpiration()
    {
        var result = await _cache.GetAsync("no_expiration_key");
    }

    [Benchmark]
    public async Task GetAsync_SlidingExpiration()
    {
        var result = await _cache.GetAsync("sliding_expiration_key");
    }

    [Benchmark]
    public async Task GetAsync_AbsoluteExpiration()
    {
        var result = await _cache.GetAsync("absolute_expiration_key");
    }

    [Benchmark]
    public async Task GetAsync_BothExpirations()
    {
        var result = await _cache.GetAsync("both_expiration_key");
    }

    [Benchmark]
    public async Task GetAsync_ShortExpiration()
    {
        var result = await _cache.GetAsync("short_expiration_key");
    }

    [Benchmark]
    public async Task RefreshAsync_SlidingExpiration()
    {
        await _cache.RefreshAsync("sliding_expiration_key");
    }

    [Benchmark]
    public async Task RefreshAsync_BothExpirations()
    {
        await _cache.RefreshAsync("both_expiration_key");
    }

    [Benchmark]
    public async Task RefreshAsync_ShortExpiration()
    {
        await _cache.RefreshAsync("short_expiration_key");
    }

    [Benchmark]
    public void SetSync_NoExpiration()
    {
        var key = $"no_exp_sync_set_{Random.Shared.Next(10000)}";
        _cache.Set(key, _testData, _noExpirationOptions);
    }

    [Benchmark]
    public void SetSync_SlidingExpiration()
    {
        var key = $"sliding_exp_sync_set_{Random.Shared.Next(10000)}";
        _cache.Set(key, _testData, _slidingExpirationOptions);
    }

    [Benchmark]
    public void SetSync_AbsoluteExpiration()
    {
        var key = $"absolute_exp_sync_set_{Random.Shared.Next(10000)}";
        _cache.Set(key, _testData, _absoluteExpirationOptions);
    }

    [Benchmark]
    public void SetSync_BothExpirations()
    {
        var key = $"both_exp_sync_set_{Random.Shared.Next(10000)}";
        _cache.Set(key, _testData, _bothExpirationOptions);
    }

    [Benchmark]
    public void GetSync_SlidingExpiration()
    {
        var result = _cache.Get("sliding_expiration_key");
    }

    [Benchmark]
    public void GetSync_AbsoluteExpiration()
    {
        var result = _cache.Get("absolute_expiration_key");
    }

    [Benchmark]
    public void RefreshSync_SlidingExpiration()
    {
        _cache.Refresh("sliding_expiration_key");
    }

    [Benchmark]
    public void RefreshSync_BothExpirations()
    {
        _cache.Refresh("both_expiration_key");
    }

    public async ValueTask DisposeAsync()
    {
        if (_fixture != null)
        {
            await _fixture.DisposeAsync();
        }
    }
}