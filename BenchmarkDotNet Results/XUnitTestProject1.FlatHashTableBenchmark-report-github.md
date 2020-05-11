``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.18363.778 (1909/November2018Update/19H2)
Intel Core i7-6700HQ CPU 2.60GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.1.201
  [Host]     : .NET Core 3.1.3 (CoreCLR 4.700.20.11803, CoreFX 4.700.20.12001), X64 RyuJIT
  DefaultJob : .NET Core 3.1.3 (CoreCLR 4.700.20.11803, CoreFX 4.700.20.12001), X64 RyuJIT


```
|             Method |          Categories | seed | numElements |         Mean |      Error |     StdDev | Ratio | RatioSD |
|------------------- |-------------------- |----- |------------ |-------------:|-----------:|-----------:|------:|--------:|
|     **GroundTruthAdd** |              **Insert** |   **42** |         **100** |     **3.162 μs** |  **0.0277 μs** |  **0.0259 μs** |  **1.00** |    **0.00** |
|            VFHSAdd |              Insert |   42 |         100 |     4.497 μs |  0.0315 μs |  0.0295 μs |  1.42 |    0.01 |
|                    |                     |      |             |              |            |            |       |         |
|    RGroundTruthAdd |       ReserveInsert |   42 |         100 |     3.024 μs |  0.0159 μs |  0.0149 μs |  1.00 |    0.00 |
|           RVFHSAdd |       ReserveInsert |   42 |         100 |     2.969 μs |  0.0184 μs |  0.0172 μs |  0.98 |    0.01 |
|                    |                     |      |             |              |            |            |       |         |
| RGroundTruthAddStr | ReserveInsertString |   42 |         100 |    12.217 μs |  0.0730 μs |  0.0647 μs |  1.00 |    0.00 |
|        RVFHSAddStr | ReserveInsertString |   42 |         100 |    12.350 μs |  0.0450 μs |  0.0376 μs |  1.01 |    0.01 |
|                    |                     |      |             |              |            |            |       |         |
|     **GroundTruthAdd** |              **Insert** |   **42** |        **1000** |    **37.505 μs** |  **0.1773 μs** |  **0.1572 μs** |  **1.00** |    **0.00** |
|            VFHSAdd |              Insert |   42 |        1000 |    53.255 μs |  0.2689 μs |  0.2383 μs |  1.42 |    0.01 |
|                    |                     |      |             |              |            |            |       |         |
|    RGroundTruthAdd |       ReserveInsert |   42 |        1000 |    31.683 μs |  0.2381 μs |  0.2227 μs |  1.00 |    0.00 |
|           RVFHSAdd |       ReserveInsert |   42 |        1000 |    30.984 μs |  0.1618 μs |  0.1351 μs |  0.98 |    0.01 |
|                    |                     |      |             |              |            |            |       |         |
| RGroundTruthAddStr | ReserveInsertString |   42 |        1000 |   186.174 μs |  1.2186 μs |  1.1399 μs |  1.00 |    0.00 |
|        RVFHSAddStr | ReserveInsertString |   42 |        1000 |   233.621 μs |  0.5571 μs |  0.4652 μs |  1.26 |    0.01 |
|                    |                     |      |             |              |            |            |       |         |
|     **GroundTruthAdd** |              **Insert** |   **42** |       **10000** |   **463.567 μs** |  **3.0632 μs** |  **2.5579 μs** |  **1.00** |    **0.00** |
|            VFHSAdd |              Insert |   42 |       10000 |   644.262 μs |  8.0015 μs |  7.0931 μs |  1.39 |    0.02 |
|                    |                     |      |             |              |            |            |       |         |
|    RGroundTruthAdd |       ReserveInsert |   42 |       10000 |   372.656 μs |  2.9280 μs |  2.7389 μs |  1.00 |    0.00 |
|           RVFHSAdd |       ReserveInsert |   42 |       10000 |   315.710 μs |  2.7774 μs |  2.5980 μs |  0.85 |    0.01 |
|                    |                     |      |             |              |            |            |       |         |
| RGroundTruthAddStr | ReserveInsertString |   42 |       10000 | 2,734.716 μs | 29.8276 μs | 26.4414 μs |  1.00 |    0.00 |
|        RVFHSAddStr | ReserveInsertString |   42 |       10000 | 3,188.420 μs | 23.1936 μs | 21.6954 μs |  1.17 |    0.01 |
|                    |                     |      |             |              |            |            |       |         |
|     **GroundTruthAdd** |              **Insert** |  **432** |         **100** |     **3.292 μs** |  **0.0331 μs** |  **0.0293 μs** |  **1.00** |    **0.00** |
|            VFHSAdd |              Insert |  432 |         100 |     4.535 μs |  0.0503 μs |  0.0470 μs |  1.38 |    0.02 |
|                    |                     |      |             |              |            |            |       |         |
|    RGroundTruthAdd |       ReserveInsert |  432 |         100 |     2.960 μs |  0.0202 μs |  0.0189 μs |  1.00 |    0.00 |
|           RVFHSAdd |       ReserveInsert |  432 |         100 |     2.897 μs |  0.0138 μs |  0.0123 μs |  0.98 |    0.01 |
|                    |                     |      |             |              |            |            |       |         |
| RGroundTruthAddStr | ReserveInsertString |  432 |         100 |    11.414 μs |  0.0947 μs |  0.0886 μs |  1.00 |    0.00 |
|        RVFHSAddStr | ReserveInsertString |  432 |         100 |    12.172 μs |  0.1116 μs |  0.0989 μs |  1.07 |    0.01 |
|                    |                     |      |             |              |            |            |       |         |
|     **GroundTruthAdd** |              **Insert** |  **432** |        **1000** |    **38.037 μs** |  **0.3763 μs** |  **0.3520 μs** |  **1.00** |    **0.00** |
|            VFHSAdd |              Insert |  432 |        1000 |    46.511 μs |  0.4373 μs |  0.4090 μs |  1.22 |    0.01 |
|                    |                     |      |             |              |            |            |       |         |
|    RGroundTruthAdd |       ReserveInsert |  432 |        1000 |    30.920 μs |  0.2557 μs |  0.2392 μs |  1.00 |    0.00 |
|           RVFHSAdd |       ReserveInsert |  432 |        1000 |    31.137 μs |  0.3005 μs |  0.2811 μs |  1.01 |    0.01 |
|                    |                     |      |             |              |            |            |       |         |
| RGroundTruthAddStr | ReserveInsertString |  432 |        1000 |   184.340 μs |  1.4965 μs |  1.3999 μs |  1.00 |    0.00 |
|        RVFHSAddStr | ReserveInsertString |  432 |        1000 |   245.756 μs |  1.2496 μs |  1.0435 μs |  1.33 |    0.01 |
|                    |                     |      |             |              |            |            |       |         |
|     **GroundTruthAdd** |              **Insert** |  **432** |       **10000** |   **458.351 μs** |  **2.4181 μs** |  **2.2619 μs** |  **1.00** |    **0.00** |
|            VFHSAdd |              Insert |  432 |       10000 |   652.913 μs |  5.6084 μs |  4.9717 μs |  1.42 |    0.01 |
|                    |                     |      |             |              |            |            |       |         |
|    RGroundTruthAdd |       ReserveInsert |  432 |       10000 |   368.031 μs |  3.1926 μs |  2.9864 μs |  1.00 |    0.00 |
|           RVFHSAdd |       ReserveInsert |  432 |       10000 |   313.505 μs |  3.1039 μs |  2.9034 μs |  0.85 |    0.01 |
|                    |                     |      |             |              |            |            |       |         |
| RGroundTruthAddStr | ReserveInsertString |  432 |       10000 | 2,722.400 μs | 14.1230 μs | 12.5197 μs |  1.00 |    0.00 |
|        RVFHSAddStr | ReserveInsertString |  432 |       10000 | 3,183.175 μs | 26.3463 μs | 24.6443 μs |  1.17 |    0.01 |
