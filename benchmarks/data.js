window.BENCHMARK_DATA = {
  "lastUpdate": 1752770940407,
  "repoUrl": "https://github.com/leonibr/community-extensions-cache-postgres",
  "entries": {
    "core-benchmark": [
      {
        "commit": {
          "author": {
            "email": "marques.ashley@gmail.com",
            "name": "Ashley Marques",
            "username": "leonibr"
          },
          "committer": {
            "email": "marques.ashley@gmail.com",
            "name": "Ashley Marques",
            "username": "leonibr"
          },
          "distinct": true,
          "id": "670cbd90360058cc0bfadcc295eda8541e943d1c",
          "message": "chore: refine CoreOperationsBenchmark description formatting in Program.cs\n\n- Removed unnecessary space before the estimated execution time in the benchmark description for CoreOperationsBenchmark to enhance clarity.",
          "timestamp": "2025-07-17T13:47:53-03:00",
          "tree_id": "bee018a0806f81c117fefe3b71f9cf061f700b14",
          "url": "https://github.com/leonibr/community-extensions-cache-postgres/commit/670cbd90360058cc0bfadcc295eda8541e943d1c"
        },
        "date": 1752770940035,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Benchmarks.UseCases.CoreOperationsBenchmark.SetAsync",
            "value": 763150.9,
            "unit": "ns",
            "range": "± 62259.33207372743"
          },
          {
            "name": "Benchmarks.UseCases.CoreOperationsBenchmark.GetAsync_Hit",
            "value": 1211053.8,
            "unit": "ns",
            "range": "± 104036.6438646606"
          },
          {
            "name": "Benchmarks.UseCases.CoreOperationsBenchmark.GetAsync_Miss",
            "value": 1084856.8,
            "unit": "ns",
            "range": "± 56340.23849947548"
          },
          {
            "name": "Benchmarks.UseCases.CoreOperationsBenchmark.RefreshAsync",
            "value": 737367.8,
            "unit": "ns",
            "range": "± 57962.85253706319"
          },
          {
            "name": "Benchmarks.UseCases.CoreOperationsBenchmark.RemoveAsync",
            "value": 638628.4,
            "unit": "ns",
            "range": "± 29918.272894485217"
          },
          {
            "name": "Benchmarks.UseCases.CoreOperationsBenchmark.SetSync",
            "value": 581292.625,
            "unit": "ns",
            "range": "± 26002.918514425568"
          },
          {
            "name": "Benchmarks.UseCases.CoreOperationsBenchmark.GetSync_Hit",
            "value": 1141982.4,
            "unit": "ns",
            "range": "± 42737.98663224296"
          },
          {
            "name": "Benchmarks.UseCases.CoreOperationsBenchmark.GetSync_Miss",
            "value": 1014091.8,
            "unit": "ns",
            "range": "± 39026.20401673157"
          },
          {
            "name": "Benchmarks.UseCases.CoreOperationsBenchmark.RefreshSync",
            "value": 665702.6666666666,
            "unit": "ns",
            "range": "± 31622.735843851337"
          },
          {
            "name": "Benchmarks.UseCases.CoreOperationsBenchmark.RemoveSync",
            "value": 573315.3,
            "unit": "ns",
            "range": "± 23315.57965443326"
          }
        ]
      }
    ]
  }
}