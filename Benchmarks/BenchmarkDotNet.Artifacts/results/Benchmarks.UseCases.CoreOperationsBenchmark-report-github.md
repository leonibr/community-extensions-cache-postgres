```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26100.4652)
AMD Ryzen 7 5700G with Radeon Graphics, 1 CPU, 16 logical and 8 physical cores
.NET SDK 9.0.301
  [Host] : .NET 9.0.6 (9.0.625.26613), X64 RyuJIT AVX2

Runtime=.NET 9.0  Toolchain=InProcessEmitToolchain  InvocationCount=1  
MaxIterationCount=10  MinIterationCount=3  UnrollFactor=1  
WarmupCount=2  

```
| Method        | Mean       | Error     | StdDev    | Min        | Max        | Median     | P90        | P95        | Ratio | RatioSD | Rank | Baseline | Allocated | Alloc Ratio |
|-------------- |-----------:|----------:|----------:|-----------:|-----------:|-----------:|-----------:|-----------:|------:|--------:|-----:|--------- |----------:|------------:|
| SetAsync      |   803.8 μs | 201.99 μs | 133.60 μs |   588.8 μs | 1,039.8 μs |   775.9 μs |   979.3 μs | 1,009.5 μs |  1.03 |    0.23 |    1 | Yes      |   8.99 KB |        1.00 |
| GetAsync_Hit  | 1,426.4 μs | 261.88 μs | 173.22 μs | 1,166.7 μs | 1,775.2 μs | 1,429.9 μs | 1,581.4 μs | 1,678.3 μs |  1.82 |    0.36 |    2 | No       |   9.97 KB |        1.11 |
| GetAsync_Miss | 1,467.8 μs |  48.84 μs |  29.06 μs | 1,430.1 μs | 1,523.8 μs | 1,457.7 μs | 1,503.6 μs | 1,513.7 μs |  1.87 |    0.30 |    2 | No       |  10.46 KB |        1.16 |
| RefreshAsync  |   945.8 μs |  56.25 μs |  37.20 μs |   878.4 μs |   993.6 μs |   951.6 μs |   978.9 μs |   986.3 μs |  1.21 |    0.20 |    1 | No       |  12.62 KB |        1.40 |
| RemoveAsync   |   803.8 μs | 175.94 μs | 116.37 μs |   637.5 μs |   996.0 μs |   799.1 μs |   926.8 μs |   961.4 μs |  1.03 |    0.22 |    1 | No       |   7.45 KB |        0.83 |
| SetSync       |   735.8 μs | 152.43 μs | 100.83 μs |   609.1 μs |   895.4 μs |   715.9 μs |   851.9 μs |   873.7 μs |  0.94 |    0.19 |    1 | No       |   8.34 KB |        0.93 |
| GetSync_Hit   | 1,369.8 μs | 230.13 μs | 152.22 μs | 1,143.3 μs | 1,541.3 μs | 1,382.5 μs | 1,528.8 μs | 1,535.0 μs |  1.75 |    0.34 |    2 | No       |   8.23 KB |        0.92 |
| GetSync_Miss  | 1,354.3 μs | 231.69 μs | 153.25 μs | 1,135.2 μs | 1,574.7 μs | 1,381.6 μs | 1,518.0 μs | 1,546.3 μs |  1.73 |    0.33 |    2 | No       |   7.84 KB |        0.87 |
| RefreshSync   |   796.1 μs | 190.78 μs | 126.19 μs |   601.3 μs | 1,000.4 μs |   828.6 μs |   899.1 μs |   949.7 μs |  1.02 |    0.22 |    1 | No       |   5.77 KB |        0.64 |
| RemoveSync    |   686.6 μs | 171.35 μs | 113.34 μs |   580.6 μs |   869.3 μs |   625.0 μs |   836.9 μs |   853.1 μs |  0.88 |    0.20 |    1 | No       |    5.8 KB |        0.65 |
