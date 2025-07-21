using BenchmarkDotNet.Attributes;
using Benchmarks.Fixtures;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;
using System.Text.Json;

namespace Benchmarks.UseCases;

/// <summary>
/// <para><strong>Bulk Operations Benchmark Suite</strong></para>
/// <para>
/// This comprehensive benchmark class evaluates the performance of PostgreSQL distributed cache 
/// under various bulk operation scenarios and high-throughput workloads. It's designed to help 
/// identify optimal strategies for applications that need to perform large-scale caching operations.
/// </para>
/// <para><strong>Configuration:</strong></para>
/// <list type="bullet">
/// <item><strong>Bulk Sizes:</strong> 10, 50, 100, 500 operations per test</item>
/// <item><strong>Runtime:</strong> .NET 9.0</item>
/// <item><strong>Metrics:</strong> Memory usage, execution time (mean, median, std dev, min, max)</item>
/// <item><strong>Test Data:</strong> Both simple byte arrays and complex JSON objects</item>
/// </list>
/// 
/// <para><strong>Test Scenarios Covered:</strong></para>
/// <list type="bullet">
/// <item><strong>Sequential vs Parallel Operations:</strong> Compares performance between sequential and parallel execution patterns</item>
/// <item><strong>CRUD Operations at Scale:</strong> Tests Set, Get, Refresh, and Remove operations with bulk data</item>
/// <item><strong>JSON Serialization Performance:</strong> Evaluates overhead of storing/retrieving complex objects</item>
/// <item><strong>Mixed Workload Simulation:</strong> Tests realistic scenarios with combined operation types</item>
/// <item><strong>High-Throughput Scenarios:</strong> Stress tests the cache under heavy concurrent load</item>
/// </list>
/// 
/// 
/// </summary>
[MemoryDiagnoser]
[RankColumn]
[MeanColumn, MedianColumn, StdDevColumn, MinColumn, MaxColumn]
public class BulkOperationsBenchmark : IAsyncDisposable
{
    private PostgreSqlBenchmarkFixture _fixture = null!;
    private IDistributedCache _cache = null!;
    private readonly byte[] _testData = Encoding.UTF8.GetBytes("This is a test cache value for bulk operations benchmarking purposes.");
    private readonly DistributedCacheEntryOptions _defaultOptions = new()
    {
        SlidingExpiration = TimeSpan.FromMinutes(30)
    };

    [Params(10, 50, 100, 500)]
    public int BulkSize { get; set; }

    // Complex object for JSON serialization benchmarks
    private readonly TestObject _complexObject = new()
    {
        Id = 12345,
        Name = "Test Object for Bulk Operations",
        Description = "This is a more complex object that will be serialized to JSON and stored in the cache.",
        CreatedAt = DateTime.UtcNow,
        IsActive = true,
        Tags = ["benchmark", "test", "performance", "cache"],
        Metadata = new()
        {
            { "version", "1.0.0" },
            { "environment", "benchmark" },
            { "priority", 5 }
        }
    };

    [GlobalSetup]
    public void GlobalSetup()
    {
        _fixture = new PostgreSqlBenchmarkFixture();
        _cache = _fixture.InitializeAsync().GetAwaiter().GetResult();

        // Pre-populate cache with some data for bulk read operations
        for (int i = 0; i < 1000; i++)
        {
            _cache.Set($"bulk_read_key_{i}", _testData, _defaultOptions);
        }
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _fixture.DisposeAsync().GetAwaiter().GetResult();
    }

    [IterationSetup]
    public async Task IterationSetup()
    {
        // Clean up and re-populate cache for consistent benchmarking
        await _fixture.CleanupAsync();

        for (int i = 0; i < Math.Min(BulkSize * 2, 500); i++)
        {
            _cache.Set($"bulk_read_key_{i}", _testData, _defaultOptions);
        }
    }

    [Benchmark(Baseline = true)]
    public string BulkSetSequential()
    {
        var baseKey = $"bulk_set_seq_{Random.Shared.Next(10000)}";

        for (int i = 0; i < BulkSize; i++)
        {
            var key = $"{baseKey}_{i}";
            _cache.Set(key, _testData, _defaultOptions);
        }
        return "BulkSetSequential";
    }

    [Benchmark]
    public async Task BulkSetParallel()
    {
        var baseKey = $"bulk_set_par_{Random.Shared.Next(10000)}";

        // Using Parallel.ForAsync
        await Parallel.ForAsync(0, BulkSize, async (i, ct) =>
        {
            var key = $"{baseKey}_{i}";
            await _cache.SetAsync(key, _testData, _defaultOptions, ct);
        });

        // Alternative approach using Task array (previous implementation):
        // var tasks = new Task[BulkSize];
        // for (int i = 0; i < BulkSize; i++)
        // {
        //     var key = $"{baseKey}_{i}";
        //     tasks[i] = _cache.SetAsync(key, _testData, _defaultOptions);
        // }
        // await Task.WhenAll(tasks);
    }

    [Benchmark]
    public string BulkGetSequential()
    {
        for (int i = 0; i < BulkSize; i++)
        {
            var keyIndex = i % Math.Min(BulkSize * 2, 500);
            _ = _cache.Get($"bulk_read_key_{keyIndex}");
        }

        return nameof(BulkGetSequential);
    }

    [Benchmark]
    public async Task<string> BulkGetParallel()
    {
        await Parallel.ForAsync(0, BulkSize, async (i, ct) =>
        {
            var keyIndex = i % Math.Min(BulkSize * 2, 500);
            var key = $"bulk_read_key_{keyIndex}";
            _ = await _cache.GetAsync(key, ct);
        });
        return nameof(BulkGetParallel);
    }

    [Benchmark]
    public async Task<string> BulkMixedOperationsSequential()
    {
        var baseKey = $"bulk_mixed_seq_{Random.Shared.Next(10000)}";

        for (int i = 0; i < BulkSize; i++)
        {
            var key = $"{baseKey}_{i}";
            var operationType = i % 4;

            switch (operationType)
            {
                case 0:
                    await _cache.SetAsync(key, _testData, _defaultOptions);
                    break;
                case 1:
                    await _cache.GetAsync(key);
                    break;
                case 2:
                    await _cache.RefreshAsync(key);
                    break;
                case 3:
                    await _cache.RemoveAsync(key);
                    break;
            }
        }
        return nameof(BulkMixedOperationsSequential);
    }

    [Benchmark]
    public async Task<string> BulkMixedOperationsParallel()
    {
        var baseKey = $"bulk_mixed_par_{Random.Shared.Next(10000)}";

        await Parallel.ForAsync(0, BulkSize, async (i, ct) =>
        {
            var key = $"{baseKey}_{i}";
            var operationType = i % 4;

            await (operationType switch
            {
                0 => _cache.SetAsync(key, _testData, _defaultOptions, ct),
                1 => GetAndIgnoreResult(key, ct),
                2 => _cache.RefreshAsync(key, ct),
                3 => _cache.RemoveAsync(key, ct),
                _ => Task.CompletedTask
            });
        });
        return nameof(BulkMixedOperationsParallel);
    }

    [Benchmark]
    public async Task<string> BulkJsonSerializationSet()
    {
        var baseKey = $"bulk_json_set_{Random.Shared.Next(10000)}";

        await Parallel.ForAsync(0, BulkSize, async (i, ct) =>
        {
            var key = $"{baseKey}_{i}";
            var modifiedObject = new TestObject
            {
                Id = _complexObject.Id + i,
                Name = $"{_complexObject.Name} #{i}",
                Description = _complexObject.Description,
                CreatedAt = _complexObject.CreatedAt.AddMinutes(i),
                IsActive = _complexObject.IsActive,
                Tags = _complexObject.Tags,
                Metadata = new Dictionary<string, object>(_complexObject.Metadata)
                {
                    { "index", i }
                }
            };

            var jsonData = JsonSerializer.SerializeToUtf8Bytes(modifiedObject);
            await _cache.SetAsync(key, jsonData, _defaultOptions, ct);
        });
        return nameof(BulkJsonSerializationSet);
    }

    [Benchmark]
    public async Task<string> BulkJsonSerializationGet()
    {
        // First, set some JSON data
        var baseKey = $"bulk_json_get_{Random.Shared.Next(10000)}";
        var jsonData = JsonSerializer.SerializeToUtf8Bytes(_complexObject);

        for (int i = 0; i < BulkSize; i++)
        {
            var key = $"{baseKey}_{i}";
            await _cache.SetAsync(key, jsonData, _defaultOptions);
        }

        // Then, retrieve and deserialize them in parallel
        await Parallel.ForAsync(0, BulkSize, async (i, ct) =>
        {
            var key = $"{baseKey}_{i}";
            _ = await GetAndDeserializeObject(key, ct);
        });
        return nameof(BulkJsonSerializationGet);
    }

    [Benchmark]
    public async Task<string> BulkRefreshOperations()
    {
        await Parallel.ForAsync(0, BulkSize, async (i, ct) =>
         {
             var keyIndex = i % Math.Min(BulkSize * 2, 500);
             var key = $"bulk_read_key_{keyIndex}";
             await _cache.RefreshAsync(key, ct);
         });
        return nameof(BulkRefreshOperations);
    }

    [Benchmark]
    public async Task<string> BulkRemoveOperations()
    {
        var baseKey = $"bulk_remove_{Random.Shared.Next(10000)}";

        // First, set the keys
        for (int i = 0; i < BulkSize; i++)
        {
            var key = $"{baseKey}_{i}";
            await _cache.SetAsync(key, _testData, _defaultOptions);
        }

        // Then, remove them in parallel
        await Parallel.ForAsync(0, BulkSize, async (i, ct) =>
        {
            var key = $"{baseKey}_{i}";
            await _cache.RemoveAsync(key, ct);
        });
        return nameof(BulkRemoveOperations);
    }

    [Benchmark]
    public async Task<string> HighThroughputScenario()
    {
        var totalOperations = BulkSize * 4; // 4 operations per bulk size
        var baseKey = $"high_throughput_{Random.Shared.Next(10000)}";

        await Parallel.ForAsync(0, totalOperations, async (i, ct) =>
        {
            var key = $"{baseKey}_{i}";
            var operationType = i % 8;

            await (operationType switch
            {
                0 or 1 or 2 => _cache.SetAsync(key, _testData, _defaultOptions, ct), // 3/8 sets
                3 or 4 or 5 => GetAndIgnoreResult(key, ct), // 3/8 gets
                6 => _cache.RefreshAsync(key, ct), // 1/8 refreshes
                7 => _cache.RemoveAsync(key, ct), // 1/8 removes
                _ => Task.CompletedTask
            });
        });
        return nameof(HighThroughputScenario);
    }

    private async Task<string> GetAndIgnoreResult(string key, CancellationToken cancellationToken = default)
    {
        await _cache.GetAsync(key, cancellationToken);
        return key;
    }

    private async Task<TestObject?> GetAndDeserializeObject(string key, CancellationToken cancellationToken = default)
    {
        var data = await _cache.GetAsync(key, cancellationToken);
        if (data == null)
            return null;

        return JsonSerializer.Deserialize<TestObject>(data);
    }

    public async ValueTask DisposeAsync()
    {
        if (_fixture != null)
        {
            await _fixture.DisposeAsync();
        }
    }

    public class TestObject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public string[] Tags { get; set; } = Array.Empty<string>();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}