``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.18363.778 (1909/November2018Update/19H2)
Intel Core i7-6700HQ CPU 2.60GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.1.201
  [Host]     : .NET Core 3.1.3 (CoreCLR 4.700.20.11803, CoreFX 4.700.20.12001), X64 RyuJIT
  DefaultJob : .NET Core 3.1.3 (CoreCLR 4.700.20.11803, CoreFX 4.700.20.12001), X64 RyuJIT


```
|    Method | Seed | NumEntries |         Mean |        Error |      StdDev | Ratio | RatioSD |
|---------- |----- |----------- |-------------:|-------------:|------------:|------:|--------:|
|  **GTRemove** |   **42** |         **12** |     **690.2 ns** |     **12.32 ns** |    **10.29 ns** |  **1.00** |    **0.00** |
| VFHRemove |   42 |         12 |     613.1 ns |      2.70 ns |     2.39 ns |  0.89 |    0.01 |
|           |      |            |              |              |             |       |         |
|  **GTRemove** |   **42** |         **34** |   **1,002.7 ns** |      **8.28 ns** |     **7.75 ns** |  **1.00** |    **0.00** |
| VFHRemove |   42 |         34 |     839.7 ns |     14.67 ns |    13.72 ns |  0.84 |    0.01 |
|           |      |            |              |              |             |       |         |
|  **GTRemove** |   **42** |       **2345** |  **41,874.5 ns** |    **239.81 ns** |   **212.59 ns** |  **1.00** |    **0.00** |
| VFHRemove |   42 |       2345 |  29,329.1 ns |    133.07 ns |   124.47 ns |  0.70 |    0.00 |
|           |      |            |              |              |             |       |         |
|  **GTRemove** |   **42** |      **33456** | **532,935.8 ns** | **10,081.03 ns** | **8,936.57 ns** |  **1.00** |    **0.00** |
| VFHRemove |   42 |      33456 | 476,910.3 ns |  1,977.06 ns | 1,849.35 ns |  0.90 |    0.02 |
|           |      |            |              |              |             |       |         |
|  **GTRemove** |  **345** |         **12** |     **647.6 ns** |      **2.66 ns** |     **2.36 ns** |  **1.00** |    **0.00** |
| VFHRemove |  345 |         12 |     604.7 ns |      2.79 ns |     2.61 ns |  0.93 |    0.00 |
|           |      |            |              |              |             |       |         |
|  **GTRemove** |  **345** |         **34** |     **939.3 ns** |      **4.76 ns** |     **4.22 ns** |  **1.00** |    **0.00** |
| VFHRemove |  345 |         34 |     822.2 ns |      9.26 ns |     8.66 ns |  0.87 |    0.01 |
|           |      |            |              |              |             |       |         |
|  **GTRemove** |  **345** |       **2345** |  **41,867.4 ns** |    **821.35 ns** |   **878.84 ns** |  **1.00** |    **0.00** |
| VFHRemove |  345 |       2345 |  29,789.3 ns |    356.97 ns |   333.91 ns |  0.71 |    0.02 |
|           |      |            |              |              |             |       |         |
|  **GTRemove** |  **345** |      **33456** | **540,482.4 ns** |  **2,203.42 ns** | **2,061.08 ns** |  **1.00** |    **0.00** |
| VFHRemove |  345 |      33456 | 437,803.8 ns |  3,002.26 ns | 2,808.31 ns |  0.81 |    0.01 |
|           |      |            |              |              |             |       |         |
|  **GTRemove** |  **765** |         **12** |     **670.6 ns** |      **4.07 ns** |     **3.61 ns** |  **1.00** |    **0.00** |
| VFHRemove |  765 |         12 |     603.4 ns |      1.96 ns |     1.83 ns |  0.90 |    0.01 |
|           |      |            |              |              |             |       |         |
|  **GTRemove** |  **765** |         **34** |     **999.8 ns** |     **11.89 ns** |    **11.12 ns** |  **1.00** |    **0.00** |
| VFHRemove |  765 |         34 |     834.0 ns |     11.06 ns |     9.80 ns |  0.83 |    0.02 |
|           |      |            |              |              |             |       |         |
|  **GTRemove** |  **765** |       **2345** |  **36,980.8 ns** |    **116.44 ns** |   **108.91 ns** |  **1.00** |    **0.00** |
| VFHRemove |  765 |       2345 |  28,545.0 ns |    114.90 ns |   101.86 ns |  0.77 |    0.00 |
|           |      |            |              |              |             |       |         |
|  **GTRemove** |  **765** |      **33456** | **535,628.5 ns** |  **2,174.69 ns** | **2,034.21 ns** |  **1.00** |    **0.00** |
| VFHRemove |  765 |      33456 | 430,218.6 ns |  4,734.93 ns | 4,429.05 ns |  0.80 |    0.01 |