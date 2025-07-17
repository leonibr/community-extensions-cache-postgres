using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Loggers;
using Benchmarks.UseCases;

// Create a custom configuration for our benchmarks
var config = ManualConfig.Create(DefaultConfig.Instance)
    .AddJob(Job.Default
        .WithRuntime(BenchmarkDotNet.Environments.CoreRuntime.Core90)
        .WithToolchain(InProcessEmitToolchain.Instance)
        .WithMinIterationCount(3)
        .WithMaxIterationCount(10)
        .WithWarmupCount(2))
    .AddColumn(StatisticColumn.Mean)
    .AddColumn(StatisticColumn.Error)
    .AddColumn(StatisticColumn.StdDev)
    .AddColumn(StatisticColumn.Min)
    .AddColumn(StatisticColumn.Max)
    .AddColumn(StatisticColumn.P90)
    .AddColumn(StatisticColumn.P95)
    .AddColumn(BaselineColumn.Default)
    .AddColumn(RankColumn.Arabic)
    .AddDiagnoser(MemoryDiagnoser.Default)
    .AddExporter(MarkdownExporter.GitHub)
    .AddExporter(HtmlExporter.Default)
    .AddExporter(CsvExporter.Default)
    .AddExporter(JsonExporter.Default)
    .AddLogger(ConsoleLogger.Default);

Console.WriteLine("PostgreSQL Distributed Cache Benchmarks");
Console.WriteLine("========================================");
Console.WriteLine();
Console.WriteLine("Available benchmark classes:");
Console.WriteLine("1. CoreOperationsBenchmark - Basic cache operations (Get, Set, Delete, Refresh) [~10 minutes]");
Console.WriteLine("2. DataSizeBenchmark - Performance with different payload sizes [~10 minutes]");
Console.WriteLine("3. ExpirationBenchmark - Different expiration strategies [~10 minutes]");
Console.WriteLine("4. ConcurrencyBenchmark - Concurrent access patterns [~15 minutes]");
Console.WriteLine("5. BulkOperationsBenchmark - Bulk operations and high-throughput scenarios [~15 minutes]");
Console.WriteLine();

// Check if user provided specific benchmark class
if (args.Length > 0)
{
    var benchmarkType = args[0].ToLowerInvariant() switch
    {
        "core" or "coreoperations" => typeof(CoreOperationsBenchmark),
        "datasize" or "size" => typeof(DataSizeBenchmark),
        "expiration" or "expire" => typeof(ExpirationBenchmark),
        "concurrency" or "concurrent" => typeof(ConcurrencyBenchmark),
        "bulk" or "bulkoperations" => typeof(BulkOperationsBenchmark),
        _ => null
    };

    if (benchmarkType != null)
    {
        Console.WriteLine($"Running {benchmarkType.Name}...");
        BenchmarkRunner.Run(benchmarkType, config);
    }
    else
    {
        Console.WriteLine($"Unknown benchmark type: {args[0]}");
        Console.WriteLine("Use one of: core, datasize, expiration, concurrency, bulk");
    }
}
else
{
    // Run all benchmarks
    Console.WriteLine("Running all benchmarks... This may take a while.");
    Console.WriteLine("To run specific benchmarks, use: dotnet run -- <benchmark-name>");
    Console.WriteLine();

    BenchmarkRunner.Run<CoreOperationsBenchmark>(config);
    BenchmarkRunner.Run<DataSizeBenchmark>(config);
    BenchmarkRunner.Run<ExpirationBenchmark>(config);
    BenchmarkRunner.Run<ConcurrencyBenchmark>(config);
    BenchmarkRunner.Run<BulkOperationsBenchmark>(config);
}
