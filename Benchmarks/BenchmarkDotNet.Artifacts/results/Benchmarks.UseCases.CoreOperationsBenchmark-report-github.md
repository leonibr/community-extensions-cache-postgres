```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26100.4652)
AMD Ryzen 7 5700G with Radeon Graphics, 1 CPU, 16 logical and 8 physical cores
.NET SDK 9.0.301
  [Host] : .NET 9.0.6 (9.0.625.26613), X64 RyuJIT AVX2

Runtime=.NET 9.0  Toolchain=InProcessEmitToolchain  InvocationCount=1  
MaxIterationCount=10  MinIterationCount=3  UnrollFactor=1  
WarmupCount=2  

```
| Method        | Mean       | Error    | StdDev    | Min        | Max        | Median     | P90        | P95        | Ratio | RatioSD | Rank | Baseline | Allocated | Alloc Ratio |
|-------------- |-----------:|---------:|----------:|-----------:|-----------:|-----------:|-----------:|-----------:|------:|--------:|-----:|--------- |----------:|------------:|
| SetAsync      |   845.9 μs | 171.6 μs | 102.12 μs |   707.0 μs | 1,005.4 μs |   823.5 μs |   955.2 μs |   980.3 μs |  1.01 |    0.16 |    1 | Yes      |   9.25 KB |        1.00 |
| GetAsync_Hit  | 1,598.3 μs | 207.8 μs | 123.68 μs | 1,388.4 μs | 1,765.3 μs | 1,580.7 μs | 1,742.3 μs | 1,753.8 μs |  1.91 |    0.26 |    2 | No       |  10.55 KB |        1.14 |
| GetAsync_Miss | 1,414.2 μs | 193.9 μs | 128.26 μs | 1,182.5 μs | 1,601.3 μs | 1,408.5 μs | 1,541.7 μs | 1,571.5 μs |  1.69 |    0.24 |    2 | No       |  10.51 KB |        1.14 |
| RefreshAsync  |   787.0 μs | 102.3 μs |  67.64 μs |   671.9 μs |   877.6 μs |   788.1 μs |   874.6 μs |   876.1 μs |  0.94 |    0.13 |    1 | No       |   9.29 KB |        1.00 |
| RemoveAsync   |   722.7 μs | 146.9 μs |  76.82 μs |   617.1 μs |   813.6 μs |   734.5 μs |   799.1 μs |   806.4 μs |  0.87 |    0.13 |    1 | No       |   7.45 KB |        0.81 |
| SetSync       |   755.4 μs | 236.0 μs | 156.09 μs |   584.8 μs |   970.8 μs |   705.6 μs |   953.5 μs |   962.2 μs |  0.90 |    0.21 |    1 | No       |   8.63 KB |        0.93 |
| GetSync_Hit   | 1,333.1 μs | 193.7 μs | 128.10 μs | 1,192.7 μs | 1,558.2 μs | 1,307.2 μs | 1,501.9 μs | 1,530.1 μs |  1.60 |    0.23 |    2 | No       |   8.23 KB |        0.89 |
| GetSync_Miss  | 1,366.1 μs | 222.5 μs | 132.43 μs | 1,120.4 μs | 1,600.4 μs | 1,384.2 μs | 1,475.8 μs | 1,538.1 μs |  1.64 |    0.24 |    2 | No       |   8.12 KB |        0.88 |
| RefreshSync   |   900.5 μs | 104.5 μs |  62.18 μs |   805.0 μs |   995.5 μs |   911.0 μs |   956.5 μs |   976.0 μs |  1.08 |    0.14 |    1 | No       |    5.2 KB |        0.56 |
| RemoveSync    |   803.9 μs | 120.9 μs |  79.95 μs |   699.4 μs |   947.9 μs |   801.1 μs |   877.9 μs |   912.9 μs |  0.96 |    0.14 |    1 | No       |   5.52 KB |        0.60 |
