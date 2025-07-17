using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;
using System.Collections.Concurrent;
using System.Linq;
using Benchmarks.Fixtures;

namespace Benchmarks.UseCases;

/// <summary>
/// Benchmarks for cache operations under concurrent access patterns
/// </summary>
[MemoryDiagnoser]
[RankColumn]
[MeanColumn, MedianColumn, StdDevColumn, MinColumn, MaxColumn]
public class ConcurrencyBenchmark : IAsyncDisposable
{
    private PostgreSqlBenchmarkFixture _fixture = null!;
    private IDistributedCache _cache = null!;
    private readonly byte[] _testData = Encoding.UTF8.GetBytes("This is a test cache value for concurrency benchmarking purposes.");
    private readonly DistributedCacheEntryOptions _defaultOptions = new()
    {
        SlidingExpiration = TimeSpan.FromMinutes(30)
    };

    [Params(2, 4, 8, 16)]
    public int ConcurrentTasks { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _fixture = new PostgreSqlBenchmarkFixture();
        _cache = _fixture.InitializeAsync().GetAwaiter().GetResult();

        // Pre-populate cache with some data for read operations
        for (int i = 0; i < 1000; i++)
        {
            _cache.Set($"concurrent_read_key_{i}", _testData, _defaultOptions);
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
        // Clean up and re-populate cache for consistent benchmarking
        _fixture.CleanupAsync().GetAwaiter().GetResult();

        for (int i = 0; i < 100; i++)
        {
            _cache.Set($"concurrent_read_key_{i}", _testData, _defaultOptions);
        }
    }

    [Benchmark(Baseline = true)]
    public async Task<string> ConcurrentSet()
    {
        var baseKey = $"concurrent_set_{Random.Shared.Next(10000)}";

        await Parallel.ForEachAsync(
            Enumerable.Range(0, ConcurrentTasks),
            new ParallelOptions { MaxDegreeOfParallelism = ConcurrentTasks },
            async (i, cancellationToken) =>
            {
                var key = $"{baseKey}_{i}";
                await _cache.SetAsync(key, _testData, _defaultOptions, cancellationToken);
            });

        return nameof(ConcurrentSet);
    }

    [Benchmark]
    public async Task<string> ConcurrentGet()
    {
        var tasks = new Task<byte[]?>[ConcurrentTasks];

        for (int i = 0; i < ConcurrentTasks; i++)
        {
            var keyIndex = Random.Shared.Next(100);
            var key = $"concurrent_read_key_{keyIndex}";
            tasks[i] = _cache.GetAsync(key);
        }

        await Task.WhenAll(tasks);
        return nameof(ConcurrentGet);
    }

    [Benchmark]
    public async Task<string> ConcurrentMixedOperations()
    {
        var tasks = new Task[ConcurrentTasks];
        var baseKey = $"concurrent_mixed_{Random.Shared.Next(10000)}";

        for (int i = 0; i < ConcurrentTasks; i++)
        {
            var operationType = i % 4;
            var key = $"{baseKey}_{i}";

            tasks[i] = operationType switch
            {
                0 => _cache.SetAsync(key, _testData, _defaultOptions),
                1 => GetAndIgnoreResult(key),
                2 => _cache.RefreshAsync(key),
                3 => _cache.RemoveAsync(key),
                _ => Task.CompletedTask
            };
        }

        await Task.WhenAll(tasks);
        return nameof(ConcurrentMixedOperations);
    }

    [Benchmark]
    public async Task<string> ConcurrentSetSameKey()
    {
        var tasks = new Task[ConcurrentTasks];
        var sharedKey = $"shared_key_{Random.Shared.Next(1000)}";

        for (int i = 0; i < ConcurrentTasks; i++)
        {
            var uniqueData = Encoding.UTF8.GetBytes($"Data from task {i}");
            tasks[i] = _cache.SetAsync(sharedKey, uniqueData, _defaultOptions);
        }

        await Task.WhenAll(tasks);
        return nameof(ConcurrentSetSameKey);
    }

    [Benchmark]
    public async Task<string> ConcurrentGetSameKey()
    {
        var tasks = new Task<byte[]?>[ConcurrentTasks];
        var sharedKey = "concurrent_read_key_0"; // Use pre-populated key

        for (int i = 0; i < ConcurrentTasks; i++)
        {
            tasks[i] = _cache.GetAsync(sharedKey);
        }

        await Task.WhenAll(tasks);
        return nameof(ConcurrentGetSameKey);
    }

    [Benchmark]
    public async Task<string> ConcurrentRefresh()
    {
        var tasks = new Task[ConcurrentTasks];

        for (int i = 0; i < ConcurrentTasks; i++)
        {
            var keyIndex = Random.Shared.Next(100);
            var key = $"concurrent_read_key_{keyIndex}";
            tasks[i] = _cache.RefreshAsync(key);
        }

        await Task.WhenAll(tasks);
        return nameof(ConcurrentRefresh);
    }

    [Benchmark]
    public async Task<string> ConcurrentRemove()
    {
        var tasks = new Task[ConcurrentTasks];
        var baseKey = $"concurrent_remove_{Random.Shared.Next(10000)}";

        // First, set the keys
        for (int i = 0; i < ConcurrentTasks; i++)
        {
            var key = $"{baseKey}_{i}";
            await _cache.SetAsync(key, _testData, _defaultOptions);
        }

        // Then, remove them concurrently
        for (int i = 0; i < ConcurrentTasks; i++)
        {
            var key = $"{baseKey}_{i}";
            tasks[i] = _cache.RemoveAsync(key);
        }

        await Task.WhenAll(tasks);
        return nameof(ConcurrentRemove);
    }

    [Benchmark]
    public async Task<string> ConcurrentHighContentionScenario()
    {
        var tasks = new Task[ConcurrentTasks];
        var sharedKeys = new[] { "shared_key_1", "shared_key_2", "shared_key_3" };

        for (int i = 0; i < ConcurrentTasks; i++)
        {
            var taskIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                var key = sharedKeys[taskIndex % sharedKeys.Length];
                var operations = new Func<Task>[]
                {
                    () => _cache.SetAsync(key, _testData, _defaultOptions),
                    () => GetAndIgnoreResult(key),
                    () => _cache.RefreshAsync(key),
                    () => _cache.RemoveAsync(key)
                };

                var operation = operations[taskIndex % operations.Length];
                await operation();
            });
        }

        await Task.WhenAll(tasks);
        return nameof(ConcurrentHighContentionScenario);
    }

    [Benchmark]
    public async Task<string> ConcurrentBulkOperations()
    {
        var tasks = new Task[ConcurrentTasks];

        for (int i = 0; i < ConcurrentTasks; i++)
        {
            var taskIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                var batchSize = 10;
                var baseBatchKey = $"batch_{taskIndex}_{Random.Shared.Next(1000)}";

                // Each task performs a batch of operations
                for (int j = 0; j < batchSize; j++)
                {
                    var key = $"{baseBatchKey}_{j}";
                    await _cache.SetAsync(key, _testData, _defaultOptions);
                }
            });
        }

        await Task.WhenAll(tasks);
        return nameof(ConcurrentBulkOperations);
    }

    private async Task GetAndIgnoreResult(string key)
    {
        await _cache.GetAsync(key);
    }

    public async ValueTask DisposeAsync()
    {
        if (_fixture != null)
        {
            await _fixture.DisposeAsync();
        }
    }
}