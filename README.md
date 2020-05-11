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

