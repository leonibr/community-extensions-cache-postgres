# PostgreSQL Distributed Cache Benchmarks

This project contains comprehensive benchmarks for the PostgreSQL distributed cache library using BenchmarkDotNet and TestContainers.

## Overview

The benchmark suite evaluates performance across multiple dimensions:

- **Core Operations**: Basic cache operations (Get, Set, Delete, Refresh)
- **Data Size Impact**: Performance with different payload sizes
- **Expiration Strategies**: Different expiration configurations
- **Concurrency**: Performance under concurrent access
- **Bulk Operations**: High-throughput scenarios and bulk operations

## Prerequisites

- .NET 9.0 SDK
- Docker (for PostgreSQL TestContainer run)
- At least 4GB RAM available for Docker containers
- x64 platform (recommended for accurate benchmarks)

## Quick Start

### Run All Benchmarks

```bash
dotnet run --configuration Release
```

### Run Specific Benchmark

```bash
# Core operations benchmark
dotnet run --configuration Release -- core

# Data size benchmark
dotnet run --configuration Release -- datasize

# Expiration benchmark
dotnet run --configuration Release -- expiration

# Concurrency benchmark
dotnet run --configuration Release -- concurrency

# Bulk operations benchmark
dotnet run --configuration Release -- bulk
```

## Benchmark Descriptions

### 1. CoreOperationsBenchmark

Tests the fundamental cache operations to establish baseline performance.

**Operations Tested:**

- `SetAsync` / `SetSync` - Adding new cache entries
- `GetAsync_Hit` / `GetSync_Hit` - Retrieving existing entries
- `GetAsync_Miss` / `GetSync_Miss` - Attempting to retrieve non-existent entries
- `RefreshAsync` / `RefreshSync` - Updating expiration times
- `RemoveAsync` / `RemoveSync` - Deleting cache entries

**Key Metrics:**

- Mean execution time per operation
- Memory allocations
- Throughput (operations per second)

### 2. DataSizeBenchmark

Evaluates how payload size affects cache performance.

**Payload Sizes:**

- Small: 1 KB
- Medium: 10 KB
- Large: 100 KB
- Extra Large: 1 MB

**Operations Tested:**

- Set operations with different payload sizes
- Get operations with different payload sizes
- Both async and sync variants

**Key Insights:**

- Network I/O impact on larger payloads
- Memory usage patterns
- PostgreSQL BYTEA column performance

### 3. ExpirationBenchmark

Tests performance impact of different expiration strategies.

**Expiration Types:**

- No explicit expiration (uses default)
- Sliding expiration
- Absolute expiration (relative to now)
- Absolute expiration (fixed time)
- Both sliding and absolute expiration
- Short expiration periods

**Operations Tested:**

- Set operations with different expiration configurations
- Get operations (with expiration logic)
- Refresh operations (sliding expiration updates)

**Key Insights:**

- Overhead of expiration calculation
- Database query complexity impact
- Refresh operation performance

### 4. ConcurrencyBenchmark

Tests cache performance under concurrent access patterns.

**Concurrency Levels:** 2, 4, 8, 16 concurrent tasks

**Scenarios:**

- `ConcurrentSet` - Multiple simultaneous write operations
- `ConcurrentGet` - Multiple simultaneous read operations
- `ConcurrentMixedOperations` - Mixed read/write operations
- `ConcurrentSetSameKey` - Write contention on same key
- `ConcurrentGetSameKey` - Read amplification on same key
- `ConcurrentHighContentionScenario` - High contention simulation
- `ConcurrentBulkOperations` - Each task performs multiple operations

**Key Insights:**

- Database connection pooling effectiveness
- Lock contention behavior
- Scalability characteristics

### 5. BulkOperationsBenchmark

Tests high-throughput scenarios and bulk operations.

**Bulk Sizes:** 10, 50, 100, 500 operations

**Scenarios:**

- `BulkSetSequential` vs `BulkSetParallel` - Sequential vs parallel writes
- `BulkGetSequential` vs `BulkGetParallel` - Sequential vs parallel reads
- `BulkMixedOperations` - Mixed operation batches
- `BulkJsonSerialization` - Complex object serialization performance
- `BulkRefreshOperations` - Batch refresh operations
- `BulkRemoveOperations` - Batch delete operations
- `HighThroughputScenario` - Mixed high-throughput simulation

**Key Insights:**

- Parallelization benefits
- Serialization overhead
- Database throughput limits

## Understanding Results

### Key Metrics Explained

- **Mean**: Average execution time per operation
- **Error**: Standard error of the mean
- **StdDev**: Standard deviation of measurements
- **Min/Max**: Fastest and slowest recorded times
- **P90/P95**: 90th and 95th percentile response times
- **Gen 0/1/2**: Garbage collection counts
- **Allocated**: Memory allocated per operation

### Performance Baselines

Each benchmark class uses `[Benchmark(Baseline = true)]` on a representative operation. Results show:

- **Ratio**: Performance relative to baseline (lower is better)
- **Rank**: Performance ranking within the benchmark class

### Interpreting Concurrent Results

For concurrency benchmarks, pay attention to:

- **Scaling efficiency**: How performance changes with increased concurrent tasks
- **Contention indicators**: Disproportionate slowdown suggests lock contention
- **Memory pressure**: Increased allocations under concurrency

## TestContainer Setup

The benchmarks use PostgreSQL TestContainers for isolation and reproducibility:

- **Database**: PostgreSQL 16
- **Schema**: `benchmark_cache`
- **Table**: `cache_items`
- **Cleanup**: Automatic cleanup between benchmark iterations

## Configuration Options

The PostgreSQL cache is configured with:

- Default sliding expiration: 20 minutes
- Expired items deletion interval: 5 minutes
- Infrastructure creation: Enabled
- Connection pooling: Enabled via Npgsql

## Best Practices

### Running Benchmarks

1. **Use Release Configuration**: Always run with `--configuration Release`
2. **Close Other Applications**: Minimize system noise
3. **Multiple Runs**: Run benchmarks multiple times for consistency
4. **Stable Environment**: Use the same machine configuration for comparisons

### Interpreting Results

1. **Focus on Ratios**: Compare relative performance rather than absolute times
2. **Consider Percentiles**: P95 times indicate worst-case performance
3. **Monitor Memory**: High allocation rates may indicate inefficiencies
4. **Validate with Load Testing**: Supplement with realistic load testing

## Troubleshooting

### Common Issues

**Docker Not Running**

```
Error: Docker is not running or not accessible
Solution: Start Docker Desktop or Docker service
```

**Port Conflicts**

```
Error: Port 5432 is already in use
Solution: Stop other PostgreSQL instances or change port in fixture
```

**Memory Issues**

```
Error: Out of memory during bulk operations
Solution: Reduce bulk sizes or increase available memory
```

**Slow Benchmarks**

```
Issue: Benchmarks taking too long
Solution: Reduce iteration counts or run specific benchmarks
```

**Setup Method Return Type Requirements**

```
Issue: How to handle async operations in BenchmarkDotNet setup methods
Solution:
- [GlobalSetup] and [GlobalCleanup] methods MUST return void (not Task)
- [IterationSetup] and [IterationCleanup] methods can return either void or async Task
- For async operations in GlobalSetup/GlobalCleanup, use .GetAwaiter().GetResult()
- For IterationSetup/IterationCleanup, prefer async Task when awaiting async operations
- Benchmark methods can be async and return Task
```

**Setup Method Best Practices**

```
Required Patterns:
- [GlobalSetup] public void GlobalSetup() - MUST be void, use .GetAwaiter().GetResult() for async
- [GlobalCleanup] public void GlobalCleanup() - MUST be void, use .GetAwaiter().GetResult() for async
- [IterationSetup] public void IterationSetup() - can be void for fast setup
- [IterationSetup] public async Task IterationSetup() - can be async Task for async operations
- [IterationCleanup] public void IterationCleanup() - can be void for fast cleanup
- [IterationCleanup] public async Task IterationCleanup() - can be async Task for async operations
```

## Extending Benchmarks

To add new benchmarks:

1. Create a new benchmark class inheriting from `IAsyncDisposable`
2. Use `PostgreSqlBenchmarkFixture` for database setup
3. Add appropriate BenchmarkDotNet attributes
4. Update `Program.cs` to include the new benchmark
5. Document the new benchmark in this README

## Output Files

Benchmarks generate several output files:

- `BenchmarkDotNet.Artifacts/results/*.html` - HTML reports
- `BenchmarkDotNet.Artifacts/results/*.md` - Markdown reports
- `BenchmarkDotNet.Artifacts/results/*.csv` - CSV data
- `BenchmarkDotNet.Artifacts/logs/*.log` - Execution logs

## Contributing

When adding new benchmarks:

1. Follow the existing naming conventions
2. Include appropriate cleanup logic
3. Add comprehensive documentation
4. Test with different parameter values
5. Consider memory and performance implications
