```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26100.4652)
AMD Ryzen 7 5700G with Radeon Graphics, 1 CPU, 16 logical and 8 physical cores
.NET SDK 9.0.301
  [Host] : .NET 9.0.6 (9.0.625.26613), X64 RyuJIT AVX2

Runtime=.NET 9.0  Toolchain=InProcessEmitToolchain  InvocationCount=1  
MaxIterationCount=10  MinIterationCount=3  UnrollFactor=1  
WarmupCount=2  

```
| Method                        | BulkSize | Mean       | Error      | StdDev     | Min        | Max        | Median     | P90        | P95        | Ratio | RatioSD | Rank | Baseline | Allocated   | Alloc Ratio |
|------------------------------ |--------- |-----------:|-----------:|-----------:|-----------:|-----------:|-----------:|-----------:|-----------:|------:|--------:|-----:|--------- |------------:|------------:|
| **BulkSetSequential**             | **10**       |   **5.373 ms** |  **0.3056 ms** |  **0.2022 ms** |   **4.985 ms** |   **5.623 ms** |   **5.405 ms** |   **5.599 ms** |   **5.611 ms** |  **1.00** |    **0.05** |    **2** | **Yes**      |   **118.99 KB** |        **1.00** |
| BulkSetParallel               | 10       |   1.710 ms |  0.3318 ms |  0.1735 ms |   1.580 ms |   2.122 ms |   1.673 ms |   1.833 ms |   1.977 ms |  0.32 |    0.03 |    1 | No       |   102.42 KB |        0.86 |
| BulkGetSequential             | 10       |  10.319 ms |  0.3859 ms |  0.2553 ms |   9.867 ms |  10.666 ms |  10.332 ms |  10.652 ms |  10.659 ms |  1.92 |    0.08 |    3 | No       |   132.02 KB |        1.11 |
| BulkGetParallel               | 10       |   2.521 ms |  0.4472 ms |  0.2339 ms |   2.221 ms |   2.978 ms |   2.460 ms |   2.782 ms |   2.880 ms |  0.47 |    0.04 |    1 | No       |   127.55 KB |        1.07 |
| BulkMixedOperationsSequential | 10       |   7.139 ms |  0.3532 ms |  0.2336 ms |   6.744 ms |   7.483 ms |   7.138 ms |   7.432 ms |   7.457 ms |  1.33 |    0.06 |    2 | No       |   122.83 KB |        1.03 |
| BulkMixedOperationsParallel   | 10       |   2.270 ms |  0.2976 ms |  0.1557 ms |   2.030 ms |   2.505 ms |   2.284 ms |   2.418 ms |   2.461 ms |  0.42 |    0.03 |    1 | No       |   114.86 KB |        0.97 |
| BulkJsonSerializationSet      | 10       |   1.845 ms |  0.4279 ms |  0.2238 ms |   1.599 ms |   2.301 ms |   1.816 ms |   2.063 ms |   2.182 ms |  0.34 |    0.04 |    1 | No       |   118.71 KB |        1.00 |
| BulkJsonSerializationGet      | 10       |   8.382 ms |  0.5062 ms |  0.3348 ms |   7.750 ms |   8.821 ms |   8.378 ms |   8.802 ms |   8.812 ms |  1.56 |    0.08 |    2 | No       |   227.18 KB |        1.91 |
| BulkRefreshOperations         | 10       |   1.608 ms |  0.4765 ms |  0.2492 ms |   1.429 ms |   2.171 ms |   1.530 ms |   1.882 ms |   2.026 ms |  0.30 |    0.05 |    1 | No       |    91.73 KB |        0.77 |
| BulkRemoveOperations          | 10       |   7.230 ms |  0.4500 ms |  0.2678 ms |   6.829 ms |   7.748 ms |   7.174 ms |   7.477 ms |   7.612 ms |  1.35 |    0.07 |    2 | No       |   170.22 KB |        1.43 |
| HighThroughputScenario        | 10       |   6.193 ms |  0.4045 ms |  0.2407 ms |   5.834 ms |   6.567 ms |   6.127 ms |   6.510 ms |   6.539 ms |  1.15 |    0.06 |    2 | No       |   287.87 KB |        2.42 |
|                               |          |            |            |            |            |            |            |            |            |       |         |      |          |             |             |
| **BulkSetSequential**             | **50**       |  **25.984 ms** |  **1.2423 ms** |  **0.6498 ms** |  **25.074 ms** |  **26.961 ms** |  **26.044 ms** |  **26.701 ms** |  **26.831 ms** |  **1.00** |    **0.03** |    **3** | **Yes**      |   **641.13 KB** |        **1.00** |
| BulkSetParallel               | 50       |  10.243 ms |  3.5973 ms |  2.3794 ms |   6.684 ms |  12.818 ms |  10.897 ms |  12.665 ms |  12.742 ms |  0.39 |    0.09 |    1 | No       |   448.23 KB |        0.70 |
| BulkGetSequential             | 50       |  47.160 ms |  1.7149 ms |  1.0205 ms |  45.193 ms |  48.339 ms |  47.189 ms |  48.313 ms |  48.326 ms |  1.82 |    0.06 |    4 | No       |   636.39 KB |        0.99 |
| BulkGetParallel               | 50       |  14.550 ms |  2.7419 ms |  1.8136 ms |  11.246 ms |  17.260 ms |  14.669 ms |  16.894 ms |  17.077 ms |  0.56 |    0.07 |    2 | No       |   607.73 KB |        0.95 |
| BulkMixedOperationsSequential | 50       |  29.925 ms |  0.5897 ms |  0.3084 ms |  29.455 ms |  30.438 ms |  29.981 ms |  30.223 ms |  30.330 ms |  1.15 |    0.03 |    3 | No       |   659.23 KB |        1.03 |
| BulkMixedOperationsParallel   | 50       |  11.200 ms |  3.0722 ms |  2.0321 ms |   8.236 ms |  13.814 ms |  11.728 ms |  13.330 ms |  13.572 ms |  0.43 |    0.08 |    1 | No       |   710.64 KB |        1.11 |
| BulkJsonSerializationSet      | 50       |  10.582 ms |  4.3389 ms |  2.8699 ms |   5.921 ms |  13.885 ms |  11.646 ms |  13.009 ms |  13.447 ms |  0.41 |    0.11 |    1 | No       |    529.6 KB |        0.83 |
| BulkJsonSerializationGet      | 50       |  37.481 ms |  2.5776 ms |  1.5339 ms |  36.150 ms |  41.036 ms |  36.737 ms |  39.036 ms |  40.036 ms |  1.44 |    0.07 |    3 | No       |  1130.43 KB |        1.76 |
| BulkRefreshOperations         | 50       |   9.891 ms |  4.3110 ms |  2.8514 ms |   6.340 ms |  13.127 ms |  11.246 ms |  12.691 ms |  12.909 ms |  0.38 |    0.11 |    1 | No       |   729.88 KB |        1.14 |
| BulkRemoveOperations          | 50       |  31.589 ms |  1.2589 ms |  0.7491 ms |  30.371 ms |  32.474 ms |  31.368 ms |  32.373 ms |  32.424 ms |  1.22 |    0.04 |    3 | No       |   836.64 KB |        1.30 |
| HighThroughputScenario        | 50       |  35.151 ms |  3.4620 ms |  2.2899 ms |  31.581 ms |  38.957 ms |  35.378 ms |  37.683 ms |  38.320 ms |  1.35 |    0.09 |    3 | No       |  1451.52 KB |        2.26 |
|                               |          |            |            |            |            |            |            |            |            |       |         |      |          |             |             |
| **BulkSetSequential**             | **100**      |  **48.281 ms** |  **1.5626 ms** |  **1.0336 ms** |  **46.430 ms** |  **49.364 ms** |  **48.684 ms** |  **49.294 ms** |  **49.329 ms** |  **1.00** |    **0.03** |    **3** | **Yes**      |  **1304.71 KB** |        **1.00** |
| BulkSetParallel               | 100      |  15.525 ms |  1.9766 ms |  1.3074 ms |  13.143 ms |  17.610 ms |  15.427 ms |  17.136 ms |  17.373 ms |  0.32 |    0.03 |    1 | No       |   1142.2 KB |        0.88 |
| BulkGetSequential             | 100      |  95.476 ms |  3.1443 ms |  2.0797 ms |  91.312 ms |  97.950 ms |  95.643 ms |  97.585 ms |  97.767 ms |  1.98 |    0.06 |    5 | No       |  1267.56 KB |        0.97 |
| BulkGetParallel               | 100      |  27.201 ms |  4.6147 ms |  3.0523 ms |  23.494 ms |  32.038 ms |  26.616 ms |  30.668 ms |  31.353 ms |  0.56 |    0.06 |    2 | No       |  1372.41 KB |        1.05 |
| BulkMixedOperationsSequential | 100      |  61.775 ms |  2.0693 ms |  1.0823 ms |  59.697 ms |  63.040 ms |  62.162 ms |  62.693 ms |  62.867 ms |  1.28 |    0.03 |    4 | No       |  1322.13 KB |        1.01 |
| BulkMixedOperationsParallel   | 100      |  18.359 ms |  3.8843 ms |  2.5692 ms |  14.704 ms |  21.116 ms |  19.213 ms |  20.996 ms |  21.056 ms |  0.38 |    0.05 |    1 | No       |   1157.7 KB |        0.89 |
| BulkJsonSerializationSet      | 100      |  15.647 ms |  3.0677 ms |  2.0291 ms |  12.354 ms |  18.602 ms |  15.736 ms |  17.888 ms |  18.245 ms |  0.32 |    0.04 |    1 | No       |  1267.64 KB |        0.97 |
| BulkJsonSerializationGet      | 100      |  73.638 ms |  3.6737 ms |  2.1861 ms |  71.474 ms |  78.433 ms |  72.870 ms |  75.970 ms |  77.201 ms |  1.53 |    0.05 |    4 | No       |  2279.45 KB |        1.75 |
| BulkRefreshOperations         | 100      |  17.102 ms |  3.9216 ms |  2.5939 ms |  13.384 ms |  21.115 ms |  17.926 ms |  19.458 ms |  20.287 ms |  0.35 |    0.05 |    1 | No       |   1104.3 KB |        0.85 |
| BulkRemoveOperations          | 100      |  66.716 ms |  3.8045 ms |  2.2640 ms |  63.912 ms |  70.239 ms |  66.652 ms |  69.658 ms |  69.948 ms |  1.38 |    0.05 |    4 | No       |  1671.81 KB |        1.28 |
| HighThroughputScenario        | 100      |  62.843 ms |  2.5404 ms |  1.6803 ms |  60.746 ms |  65.336 ms |  62.706 ms |  65.107 ms |  65.222 ms |  1.30 |    0.04 |    4 | No       |  2951.67 KB |        2.26 |
|                               |          |            |            |            |            |            |            |            |            |       |         |      |          |             |             |
| **BulkSetSequential**             | **500**      | **236.204 ms** |  **8.4954 ms** |  **5.6192 ms** | **227.333 ms** | **245.621 ms** | **234.785 ms** | **243.177 ms** | **244.399 ms** |  **1.00** |    **0.03** |    **4** | **Yes**      |  **4418.51 KB** |        **1.00** |
| BulkSetParallel               | 500      |  68.787 ms | 11.0105 ms |  7.2827 ms |  58.052 ms |  78.987 ms |  69.416 ms |  77.554 ms |  78.270 ms |  0.29 |    0.03 |    1 | No       |  5098.57 KB |        1.15 |
| BulkGetSequential             | 500      | 475.209 ms | 12.8853 ms |  8.5228 ms | 464.869 ms | 490.897 ms | 473.537 ms | 486.280 ms | 488.589 ms |  2.01 |    0.06 |    7 | No       |  4138.73 KB |        0.94 |
| BulkGetParallel               | 500      | 125.444 ms | 17.1117 ms | 11.3183 ms | 108.428 ms | 147.300 ms | 126.269 ms | 134.134 ms | 140.717 ms |  0.53 |    0.05 |    3 | No       |  5193.02 KB |        1.18 |
| BulkMixedOperationsSequential | 500      | 299.409 ms |  6.5797 ms |  3.4413 ms | 292.033 ms | 302.522 ms | 300.081 ms | 302.297 ms | 302.410 ms |  1.27 |    0.03 |    5 | No       |  4414.59 KB |        1.00 |
| BulkMixedOperationsParallel   | 500      |  87.686 ms | 21.0731 ms | 13.9386 ms |  66.987 ms | 110.912 ms |  86.142 ms | 102.555 ms | 106.733 ms |  0.37 |    0.06 |    2 | No       |  4395.05 KB |        0.99 |
| BulkJsonSerializationSet      | 500      |  70.155 ms |  8.7116 ms |  5.7622 ms |  60.648 ms |  78.450 ms |  70.304 ms |  76.256 ms |  77.353 ms |  0.30 |    0.02 |    1 | No       |  5406.09 KB |        1.22 |
| BulkJsonSerializationGet      | 500      | 353.314 ms |  9.2540 ms |  6.1209 ms | 342.587 ms | 362.617 ms | 354.186 ms | 359.393 ms | 361.005 ms |  1.50 |    0.04 |    6 | No       |  9200.33 KB |        2.08 |
| BulkRefreshOperations         | 500      |  68.644 ms | 12.7411 ms |  8.4275 ms |  53.096 ms |  80.207 ms |  70.605 ms |  77.086 ms |  78.647 ms |  0.29 |    0.03 |    1 | No       |   4060.8 KB |        0.92 |
| BulkRemoveOperations          | 500      | 310.366 ms | 15.4764 ms | 10.2367 ms | 296.127 ms | 327.085 ms | 311.527 ms | 319.367 ms | 323.226 ms |  1.31 |    0.05 |    5 | No       |  6232.02 KB |        1.41 |
| HighThroughputScenario        | 500      | 284.293 ms | 12.7753 ms |  8.4501 ms | 273.848 ms | 300.437 ms | 282.068 ms | 295.199 ms | 297.818 ms |  1.20 |    0.04 |    5 | No       | 12796.45 KB |        2.90 |
