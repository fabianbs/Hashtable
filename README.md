# Hashtable Collections

This repository contains a new, very fast implementation of a hash set `Collections.VectorFlatHashSet<T>` and a hash map `Collections.VectorFlatHashMap<K,V>`.

The hash set supports the standard `System.Collections.Generic.ISet<T>` interface and the hash map supports the `System.Collections.Generic.IDictionary<K,V>` interface together with some additional functionality from Java (concretely from `java.utils.Map`).

## Compatibility

The library was implemented with C# 8 and .Net Core 3.1 and should work fine with any architecture which supports .Net Core 3. Other platforms are not tested.

## Implementation details

The implementation of my new hash table does not reinvent the weel. Instead it combines some tricks and features from other implementations. It uses

- Open addressing with linear probing
- Robin hood hashing with backshift removal
- Lookup optimization with SIMD vectors as in Abseil Swiss tables ([https://abseil.io/about/design/swisstables]())
- Power-of-2 groth policy with fibonaccy hashing ([https://probablydance.com/2018/06/16/fibonacci-hashing-the-optimization-that-the-world-forgot-or-a-better-alternative-to-integer-modulo/]())

## Performance

I have run several performance benchmarks ([FlatHashTableBenchmark.cs](XUnitTestProject1/FlatHashTableBenchmark.cs)) using Benchmark.Net.

These benchmarks have shown ([BenchmarkDotNet Results](BenchmarkDotNet%20Results/)) that my implementation is generally faster than 
`System.Collections.Generic.HashSet`/`System.Collections.Generic.Dictionary` except for successful insertions.

Probably the performance gain results from using SIMD instructions. So, when your machine does not support at least SSE2 instructions, you might get different results.
Because of linear probing and robin-hood hashing, the probability is high that one single SIMD lookup is suffient to find an element in the hashtable.

The reason why successful insertion is a bit slower than the .Net implementation is because of the different algorithm. I am using robin-hood hashing to minimize the variance in 
the probing-sequence length and to avoid the usage of tombstones/sentinels for deleted elements. However, this comes with a cost: When inserting a new element, other existing elements 
may be swapped with the new one and then be reinserted. Hence, it is not surprising that successful insertion is a bit slower; but as the other benchmark results show, it is worth it.