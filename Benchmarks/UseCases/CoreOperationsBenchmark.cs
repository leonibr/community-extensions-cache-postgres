using BenchmarkDotNet.Attributes;
using Benchmarks.Fixtures;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;

namespace Benchmarks.UseCases;

/// <summary>
/// <para><strong>Core Operations Benchmark Suite</strong></para>
/// <para>
/// Benchmarks for core cache operations
/// Test Data: Uses a UTF-8 encoded string of 53 bytes as test payload
/// </para>
/// <para><strong>Cache Setup</strong></para>
/// <list type="bullet">
///    <item> Pre-populates 1000 keys during global setup</item>
///    <item> Refreshes with 100 keys before each iteration</item>
///    <item> Uses 30-minute sliding expiration</item>
/// </list>
/// <para><strong>Performance Scenarios</strong></para>
/// <list type="bullet">
///    <item> Cache Hits vs Cache Misses - Important distinction for real-world performance analysis</item>
///    <item> Async vs Sync operations - Comparing different execution models</item>
///    <item> Random key access - Simulates realistic usage patterns</item>
/// </list>
/// </summary>
[MemoryDiagnoser]
[RankColumn]
[MeanColumn, MedianColumn, StdDevColumn, MinColumn, MaxColumn]
public class CoreOperationsBenchmark : IAsyncDisposable
{
    private PostgreSqlBenchmarkFixture _fixture = null!;
    private IDistributedCache _cache = null!;
    private readonly byte[] _testData = Encoding.UTF8.GetBytes("This is a test cache value for benchmarking purposes.");
    private readonly DistributedCacheEntryOptions _defaultOptions = new()
    {
        SlidingExpiration = TimeSpan.FromMinutes(30)
    };

    [GlobalSetup]
    public void GlobalSetup()
    {
        _fixture = new PostgreSqlBenchmarkFixture();
        _cache = _fixture.InitializeAsync().GetAwaiter().GetResult();

        // Pre-populate some keys for Get and Refresh benchmarks
        for (int i = 0; i < 1000; i++)
        {
            _cache.Set($"benchmark_key_{i}", _testData, _defaultOptions);
        }
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _fixture.DisposeAsync().GetAwaiter().GetResult();
    }

    [IterationSetup]
    public void IterationSetup()
    {
        // Clean up any keys that might have been created during the benchmark
        _fixture.CleanupAsync().GetAwaiter().GetResult();

        // Re-populate keys for Get and Refresh benchmarks
        for (int i = 0; i < 100; i++)
        {
            _cache.Set($"benchmark_key_{i}", _testData, _defaultOptions);
        }
    }

    [Benchmark(Baseline = true)]
    public async Task<string> SetAsync()
    {
        var key = $"set_key_{Random.Shared.Next(10000)}";
        await _cache.SetAsync(key, _testData, _defaultOptions);
        return key;
    }

    [Benchmark]
    public async Task<byte[]?> GetAsync_Hit()
    {
        var keyIndex = Random.Shared.Next(100);
        var key = $"benchmark_key_{keyIndex}";
        return await _cache.GetAsync(key);
    }

    [Benchmark]
    public async Task<string> GetAsync_Miss()
    {
        var key = $"missing_key_{Random.Shared.Next(10000)}";
        var result = await _cache.GetAsync(key);
        return key;
    }

    [Benchmark]
    public async Task<string> RefreshAsync()
    {
        var keyIndex = Random.Shared.Next(100);
        var key = $"benchmark_key_{keyIndex}";
        await _cache.RefreshAsync(key);
        return key;
    }

    [Benchmark]
    public async Task<string> RemoveAsync()
    {
        var keyIndex = Random.Shared.Next(100);
        var key = $"benchmark_key_{keyIndex}";
        await _cache.RemoveAsync(key);
        return key;
    }

    [Benchmark]
    public string SetSync()
    {
        string key = $"set_sync_key_{Random.Shared.Next(10000)}";
        _cache.Set(key, _testData, _defaultOptions);
        return key;
    }

    [Benchmark]
    public byte[]? GetSync_Hit()
    {
        var keyIndex = Random.Shared.Next(100);
        var key = $"benchmark_key_{keyIndex}";
        return _cache.Get(key);
    }

    [Benchmark]
    public byte[]? GetSync_Miss()
    {
        var key = $"missing_sync_key_{Random.Shared.Next(10000)}";
        return _cache.Get(key);
    }

    [Benchmark]
    public string RefreshSync()
    {
        var keyIndex = Random.Shared.Next(100);
        var key = $"benchmark_key_{keyIndex}";
        _cache.Refresh(key);
        return key;
    }

    [Benchmark]
    public string RemoveSync()
    {
        var keyIndex = Random.Shared.Next(100);
        var key = $"benchmark_key_{keyIndex}";
        _cache.Remove(key);
        return key;
    }

    public async ValueTask DisposeAsync()
    {
        if (_fixture != null)
        {
            await _fixture.DisposeAsync();
        }
    }
}