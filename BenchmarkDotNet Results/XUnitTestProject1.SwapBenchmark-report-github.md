``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.18363.778 (1909/November2018Update/19H2)
Intel Core i7-6700HQ CPU 2.60GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.1.201
  [Host]     : .NET Core 3.1.3 (CoreCLR 4.700.20.11803, CoreFX 4.700.20.12001), X64 RyuJIT
  DefaultJob : .NET Core 3.1.3 (CoreCLR 4.700.20.11803, CoreFX 4.700.20.12001), X64 RyuJIT


```
|    Method | seed | numSamples |      Mean |     Error |    StdDev |
|---------- |----- |----------- |----------:|----------:|----------:|
|  **RingSwap** |   **42** |       **1000** |  **7.012 μs** | **0.0793 μs** | **0.0742 μs** |
| TupleSwap |   42 |       1000 |  8.697 μs | 0.1289 μs | 0.1206 μs |
|  **RingSwap** |   **42** |      **10000** | **82.704 μs** | **0.8731 μs** | **0.8167 μs** |
| TupleSwap |   42 |      10000 | 90.583 μs | 0.8690 μs | 0.7703 μs |
|  **RingSwap** |  **432** |       **1000** |  **6.714 μs** | **0.0739 μs** | **0.0691 μs** |
| TupleSwap |  432 |       1000 |  8.457 μs | 0.0620 μs | 0.0518 μs |
|  **RingSwap** |  **432** |      **10000** | **82.382 μs** | **0.9612 μs** | **0.8991 μs** |
| TupleSwap |  432 |      10000 | 90.289 μs | 0.6995 μs | 0.5841 μs |
