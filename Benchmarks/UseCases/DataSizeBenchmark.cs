using BenchmarkDotNet.Attributes;
using Benchmarks.Fixtures;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;

namespace Benchmarks.UseCases;

/// <summary>
/// Benchmarks for PostgreSQL cache operations across varying data payload sizes.
/// Tests both synchronous and asynchronous Set/Get operations with 1KB, 10KB, 100KB, and 1MB payloads
/// </summary>
[MemoryDiagnoser]
[RankColumn]
[MeanColumn, MedianColumn, StdDevColumn, MinColumn, MaxColumn]
public class DataSizeBenchmark : IAsyncDisposable
{
    private PostgreSqlBenchmarkFixture _fixture = null!;
    private IDistributedCache _cache = null!;

    // Different payload sizes to test
    private byte[] _smallData = null!;      // 1 KB
    private byte[] _mediumData = null!;     // 10 KB
    private byte[] _largeData = null!;      // 100 KB
    private byte[] _extraLargeData = null!; // 1 MB

    private readonly DistributedCacheEntryOptions _defaultOptions = new()
    {
        SlidingExpiration = TimeSpan.FromMinutes(30)
    };

    [GlobalSetup]
    public void GlobalSetup()
    {
        _fixture = new PostgreSqlBenchmarkFixture();
        _cache = _fixture.InitializeAsync().GetAwaiter().GetResult();

        // Create test data of different sizes
        _smallData = CreateTestData(1024);        // 1 KB
        _mediumData = CreateTestData(10240);      // 10 KB
        _largeData = CreateTestData(102400);      // 100 KB
        _extraLargeData = CreateTestData(1048576); // 1 MB

        // Pre-populate cache with different sized data
        _cache.Set("small_data", _smallData, _defaultOptions);
        _cache.Set("medium_data", _mediumData, _defaultOptions);
        _cache.Set("large_data", _largeData, _defaultOptions);
        _cache.Set("extra_large_data", _extraLargeData, _defaultOptions);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _fixture.DisposeAsync().GetAwaiter().GetResult();
    }

    [IterationSetup]
    public async Task IterationSetup()
    {
        // Re-populate cache with test data
        await _cache.SetAsync("small_data", _smallData, _defaultOptions);
        await _cache.SetAsync("medium_data", _mediumData, _defaultOptions);
        await _cache.SetAsync("large_data", _largeData, _defaultOptions);
        await _cache.SetAsync("extra_large_data", _extraLargeData, _defaultOptions);
    }

    [Benchmark(Baseline = true)]
    public async Task<string> SetAsync_Small_1KB()
    {
        var key = $"small_set_{Random.Shared.Next(10000)}";
        await _cache.SetAsync(key, _smallData, _defaultOptions);
        return nameof(SetAsync_Small_1KB);
    }

    [Benchmark]
    public async Task<string> SetAsync_Medium_10KB()
    {
        var key = $"medium_set_{Random.Shared.Next(10000)}";
        await _cache.SetAsync(key, _mediumData, _defaultOptions);
        return nameof(SetAsync_Medium_10KB);
    }

    [Benchmark]
    public async Task<string> SetAsync_Large_100KB()
    {
        var key = $"large_set_{Random.Shared.Next(10000)}";
        await _cache.SetAsync(key, _largeData, _defaultOptions);
        return nameof(SetAsync_Large_100KB);
    }

    [Benchmark]
    public async Task<string> SetAsync_ExtraLarge_1MB()
    {
        var key = $"extra_large_set_{Random.Shared.Next(10000)}";
        await _cache.SetAsync(key, _extraLargeData, _defaultOptions);
        return nameof(SetAsync_ExtraLarge_1MB);
    }

    [Benchmark]
    public async Task<byte[]?> GetAsync_Small_1KB()
    {
        return await _cache.GetAsync("small_data");
    }

    [Benchmark]
    public async Task<byte[]?> GetAsync_Medium_10KB()
    {
        return await _cache.GetAsync("medium_data");
    }

    [Benchmark]
    public async Task<byte[]?> GetAsync_Large_100KB()
    {
        return await _cache.GetAsync("large_data");
    }

    [Benchmark]
    public async Task<byte[]?> GetAsync_ExtraLarge_1MB()
    {
        return await _cache.GetAsync("extra_large_data");
    }

    [Benchmark]
    public string SetSync_Small_1KB()
    {
        var key = $"small_sync_set_{Random.Shared.Next(10000)}";
        _cache.Set(key, _smallData, _defaultOptions);
        return nameof(SetSync_Small_1KB);

    }

    [Benchmark]
    public string SetSync_Medium_10KB()
    {
        var key = $"medium_sync_set_{Random.Shared.Next(10000)}";
        _cache.Set(key, _mediumData, _defaultOptions);
        return nameof(SetSync_Medium_10KB);
    }

    [Benchmark]
    public string SetSync_Large_100KB()
    {
        var key = $"large_sync_set_{Random.Shared.Next(10000)}";
        _cache.Set(key, _largeData, _defaultOptions);
        return nameof(SetSync_Large_100KB);
    }

    [Benchmark]
    public string SetSync_ExtraLarge_1MB()
    {
        var key = $"extra_large_sync_set_{Random.Shared.Next(10000)}";
        _cache.Set(key, _extraLargeData, _defaultOptions);
        return nameof(SetSync_ExtraLarge_1MB);
    }

    [Benchmark]
    public string GetSync_Small_1KB()
    {
        var result = _cache.Get("small_data");
        return nameof(GetSync_Small_1KB); // Return a value to satisfy the compiler
    }

    [Benchmark]
    public string GetSync_Medium_10KB()
    {
        var result = _cache.Get("medium_data");
        return nameof(GetSync_Medium_10KB); // Return a value to satisfy the compiler
    }

    [Benchmark]
    public string GetSync_Large_100KB()
    {
        var result = _cache.Get("large_data");
        return nameof(GetSync_Large_100KB); // Return a value to satisfy the compiler
    }

    [Benchmark]
    public string GetSync_ExtraLarge_1MB()
    {
        var result = _cache.Get("extra_large_data");
        return nameof(GetSync_ExtraLarge_1MB); // Return a value to satisfy the compiler
    }

    /// <summary>
    /// Creates test data of specified size with some variation to avoid compression
    /// </summary>
    private static byte[] CreateTestData(int sizeInBytes)
    {
        var data = new byte[sizeInBytes];
        var random = new Random(42); // Fixed seed for reproducibility

        // Fill with random data to avoid compression
        for (int i = 0; i < sizeInBytes; i++)
        {
            data[i] = (byte)(random.Next(256));
        }

        return data;
    }

    public async ValueTask DisposeAsync()
    {
        if (_fixture != null)
        {
            await _fixture.DisposeAsync();
        }
    }
}