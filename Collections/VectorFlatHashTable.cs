//  ******************************************************************************
//  * Copyright (c) 2020 Fabian Schiebel.
//  * All rights reserved. This program and the accompanying materials are made
//  * available under the terms of LICENSE.txt.
//  *
//  *****************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Simd = System.Runtime.Intrinsics.Vector128;
using SimdVector = System.Runtime.Intrinsics.Vector128<byte>;
using Sse = System.Runtime.Intrinsics.X86.Sse2;

namespace Collections {
    /// <summary>
    ///     A flat hashtable, strongly oriented at abseil swiss table. Supports the following features:
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 Store presence metadata in a separate byte-array and use SIMD instructions to check partial
    ///                 hashcodes efficiently (taken from abseil)
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>Linear probing</description>
    ///         </item>
    ///         <item>
    ///             <description>Robin hood hashing with backshift removal</description>
    ///         </item>
    ///         <item>
    ///             <description>Fibonacci hashcodes with power-of-2 growth policy</description>
    ///         </item>
    ///     </list>
    /// </summary>
    /// <typeparam name="T">The type of elements which can be stored in a <see cref="VectorFlatHashTable{T}" /></typeparam>
    public class VectorFlatHashTable<T> : VectorFlatHashTableBase {
        /// <summary>
        /// The number of elements stored in this hashtable
        /// </summary>
        private protected uint size;

        /// <summary>
        /// The comparer used to compare element and for getting their hashcodes
        /// </summary>
        private readonly IEqualityComparer<T> _cmp;

        /// <summary>
        /// The array storing the actual elements
        /// </summary>
        private T[]? _arrVal;

        /// <summary>
        /// The capacity is this hashtable. Is always a power of 2 and is used to map the hashcode to an index in <see cref="_arrVal"/>. Thus, capacity is never larger than _arrVal.Length
        /// </summary>
        private uint _capacity;

        /// <summary>
        /// The array storing the length of the probe sequence from the index, where the element is hashed to until the index, where the element is finally stored
        /// </summary>
        private byte[]? _distanceMetadata;

        /// <summary>
        /// The array storing the 7 most significant bits of the hashcode*MAGIC_NUMBER of the element stored at the respective index. Is 0, iff the slot at that index is empty
        /// </summary>
        private byte[]? _lookupMetadata;

        /// <summary>
        /// Default constructor. Does not allocate any memory (apart from retrieving the default comparer, if <paramref name="cmp"/> is null)
        /// </summary>
        /// <param name="cmp">The comparer used to compare the elements and to retrieve their hashcodes</param>
        private protected VectorFlatHashTable(IEqualityComparer<T>? cmp = null) {
            _arrVal = null;
            _lookupMetadata = null;
            _distanceMetadata = null;
            size = _capacity = 0;
            _cmp = cmp ?? EqualityComparer<T>.Default;
        }

        /// <summary>
        /// Constructor with initial capacity.
        /// </summary>
        /// <param name="initCap">A lower bound of the initial capacity. Will be rounded up to the next power of 2</param>
        /// <param name="cmp">The comparer used to compare the elements and to retrieve their hashcodes</param>
        private protected VectorFlatHashTable(uint initCap, IEqualityComparer<T>? cmp = null) {
            _cmp = cmp ?? EqualityComparer<T>.Default; // try getting the comparer before allocating
            if (initCap != 0)
                Initialize(Math.Max(4, BitHelper.NextPow2(initCap)));

            size = 0;
        }

        /// <summary>
        /// The load (<see cref="size"/> / <see cref="_capacity"/>
        /// </summary>
        public float CurrentLoad => size * 1.0f / _capacity;

        /// <summary>
        /// Provide direct access to the elements. Should not be made public
        /// </summary>
        protected ref T this[uint idx] => ref _arrVal![idx];

        /// <summary>
        /// Iterates through all elements of this hashtable and executes the <paramref name="callBack"/> on every element. The iteration order is unspecified.
        /// </summary>
        public void ForEach(Action<T> callBack) {
            if (size == 0)
                return;

            var metHash = _lookupMetadata!;
            var arr = _arrVal!;
            uint len = arr.ULength();

            if (len >= 32 && Avx2.IsSupported) {
                unsafe {
                    fixed (byte* ptr = metHash) {
                        for (uint i = 0; i < len; i += 32) {
                            // since len is always a power of 2, this load is safe
                            var vec = Avx.LoadVector256(ptr + i);
                            int mask = ~Avx2.MoveMask(Avx2.CompareEqual(vec, Vector256<byte>.Zero));
                            while (mask.Has()) {
                                uint x = mask.Next() + i;
                                callBack(arr[x]);
                            }
                        }
                    }
                }
            }
            else if (Sse.IsSupported) {
                unsafe {
                    fixed (byte* ptr = metHash) {
                        for (uint i = 0; i < len; i += VEC_LEN) {
                            var vec = Sse.LoadVector128(ptr + i);
                            int mask = ~Sse.MoveMask(Sse.CompareEqual(vec, SimdVector.Zero)) & ushort.MaxValue;
                            while (mask.Has()) {
                                uint x = mask.Next() + i;
                                callBack(arr[x]);
                            }
                        }
                    }
                }
            }
            else {
                for (uint i = 0; i < len; ++i)
                    if (metHash![i] != 0)
                        callBack(arr[i]);
            }
        }

        /// <summary>
        /// Iterates through all elements of this hashtable and executes the <paramref name="callBack"/> on every element. The iteration order is unspecified,
        /// but whenever <paramref name="callBack"/> returns false, the iteration is stopped immediately
        /// </summary>
        public uint ForEach(Func<T, bool> callBack) {
            if (size == 0)
                return 0;

            var metHash = _lookupMetadata!;
            var arr = _arrVal!;
            uint len = arr.ULength();
            uint ctr = 0;
            if (len >= 32 && Avx2.IsSupported) {
                unsafe {
                    fixed (byte* ptr = metHash) {
                        for (uint i = 0; i < len; i += 32) {
                            // since len is always a power of 2, this load is safe
                            var vec = Avx.LoadVector256(ptr + i);
                            int mask = ~Avx2.MoveMask(Avx2.CompareEqual(vec, Vector256<byte>.Zero));
                            while (mask.Has()) {
                                uint x = mask.Next() + i;
                                if (!callBack(arr[x]))
                                    return ctr;

                                ctr++;
                            }
                        }
                    }
                }
            }
            else if (Sse.IsSupported) {
                unsafe {
                    fixed (byte* ptr = metHash) {
                        for (uint i = 0; i < len; i += VEC_LEN) {
                            var vec = Sse.LoadVector128(ptr + i);
                            int mask = ~Sse.MoveMask(Sse.CompareEqual(vec, SimdVector.Zero)) & ushort.MaxValue;
                            while (mask.Has()) {
                                uint x = mask.Next() + i;
                                if (!callBack(arr[x]))
                                    return ctr;

                                ctr++;
                            }
                        }
                    }
                }
            }
            else {
                for (uint i = 0; i < len; ++i)
                    if (metHash![i] != 0) {
                        if (!callBack(arr[i]))
                            return ctr;

                        ctr++;
                    }
            }

            return ctr;
        }
        /// <summary>
        /// Ensures, that the nex <paramref name="offs"/> unique insertions of new elements will not trigger a reallocation. May reallocate the internal structures
        /// </summary>
        public bool Reserve(uint offs) {
            // allocate a bit more to prevent reallocations because of the load factor
            uint minCap = Math.Max(4, BitHelper.NextPow2((offs + size) << 1));

            if (_arrVal is null)
                Initialize(minCap);
            else if (minCap > _capacity * LOAD_FACTOR) {
                Rehash(Math.Max(minCap, BitHelper.NextPow2((uint) (_capacity / LOAD_FACTOR))));
                return true;
            }

            return false;
        }

        /// <summary>
        /// Resets the hashtable to the state of default initialization
        /// </summary>
        protected void ClearInternal() {
            _arrVal = null;
            _lookupMetadata = null;
            _distanceMetadata = null;
            _capacity = size = 0;
        }

        /// <summary>
        /// Tries to insert an element to this hashtable, which equals <paramref name="key"/>. However, when the element should be inserted actually,
        /// the value of <paramref name="ctor"/>(<paramref name="mapKey"/>, <paramref name="mapper"/>(<paramref name="mapKey"/>)) will be inserted instead.
        /// This is useful to implement <see cref="VectorFlatHashMap{K,V}.ComputeIfAbsent(K, Func{K,V})"/>
        /// </summary>
        /// <typeparam name="U">The type of <paramref name="mapKey"/></typeparam>
        /// <typeparam name="V">The result type of the mapping using <paramref name="mapper"/></typeparam>
        /// <param name="mapKey">The key used in the <see cref="VectorFlatHashMap{K,V}"/></param>
        /// <param name="key">The key used for finding the right slot in the hashtable. The hashcodes of <paramref name="key"/> and <paramref name="ctor"/>(<paramref name="mapKey"/>, <paramref name="mapper"/>(<paramref name="mapKey"/>))
        /// must be the same and the expression <see cref="_cmp"/>.Equals(<paramref name="key"/>, <paramref name="ctor"/>(<paramref name="mapKey"/>, <paramref name="mapper"/>(<paramref name="mapKey"/>)))
        /// must always evaluate to true</param>
        /// <param name="mapper">The function used to construct a value based on the key <paramref name="mapKey"/></param>
        /// <param name="ctor">The constructor function used to construct a <typeparamref name="T"/> value from the <paramref name="mapper"/> result</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        protected ref T ComputeIfAbsent<U, V>(U mapKey, T key, [NotNull] Func<U, V> mapper,
            [NotNull] Func<U, V, T> ctor) {
            if (_arrVal is null)
                Initialize();
            else if (size > LOAD_FACTOR * _capacity)
                Rehash();

            var cmp = _cmp;

            uint idx = HashSlot(key is null ? 0 : cmp.GetHashCode(key), _capacity, out byte meta);
            var arr = _arrVal!;
            var metHash = _lookupMetadata!;
            var metDist = _distanceMetadata!;

            uint len = _capacity;
            uint cap = len - 1;

            byte distance = 0;

            for (uint i = 0; i < len; ++i, idx = (idx + 1) & cap) {
                byte sHash = metHash![idx];
                if (sHash == 0) {
                    arr![idx] = ctor(mapKey, mapper(mapKey));
                    metHash[idx] = meta;
                    metDist![idx] = distance;
                    ++size;
                    return ref arr[idx];
                }

                if (sHash == meta && cmp.Equals(key, arr![idx]))
                    return ref arr[idx];

                if (metDist![idx] < distance) {
                    // robin hood
                    var insKy = ctor(mapKey, mapper(mapKey));
                    (arr![idx], key) = (insKy, arr[idx]);
                    (meta, metHash[idx]) = (metHash[idx], meta);
                    (distance, metDist[idx]) = (metDist[idx], distance);
                    //InsertInternal(key, false, idx, meta, distance);
                    InsertUnique(key, idx, meta, distance);
                    return ref _arrVal == arr ? ref arr[idx] : ref GetOrElse(insKy, ref arr[idx]);
                }

                checked {
                    ++distance;
                }
            }

            // unreachable
            return ref arr![0];
        }

        /// <summary>
        /// Tries to insert an element to this hashtable, which equals <paramref name="key"/>. If there is already a matching element <c>x</c> in the hashtable, it will be
        /// replaced by <paramref name="ctor"/>(<paramref name="mapKey"/>, <paramref name="mergerFunc"/>(<paramref name="extractor"/>(<c>x</c>), <paramref name="mapValue"/>)) Otherwise,
        /// the element <paramref name="value"/> will be inserted
        ///
        /// The type <typeparamref name="T"/> can be thought of as it were constructed by an aggregate of a key <typeparamref name="U"/> and a value <typeparamref name="V"/>. A valid example is <see cref="KVPair{K,V}"/>
        /// </summary>
        /// <typeparam name="U">The type of <paramref name="mapKey"/></typeparam>
        /// <typeparam name="V">The type of <paramref name="mapValue"/> and the type of the parameters of the merger function</typeparam>
        /// <param name="mapKey">The key used in <see cref="VectorFlatHashMap{K,V}"/></param>
        /// <param name="key">The key used for finding the right slot in the hashtable. The hashcodes of <paramref name="key"/> and <paramref name="ctor"/>(<paramref name="mapKey"/>, <paramref name="mergerFunc"/>(<paramref name="extractor"/>(<c>x</c>), <paramref name="mapValue"/>))
        /// must be the same and the expression <see cref="_cmp"/>.Equals(<paramref name="key"/>, <paramref name="ctor"/>(<paramref name="mapKey"/>, <paramref name="mergerFunc"/>(<paramref name="extractor"/>(<c>x</c>), <paramref name="mapValue"/>)))
        /// must always evaluate to true. Furthermore, the hashcodes of <paramref name="key"/> and <paramref name="value"/> must be the same and <see cref="_cmp"/>.Equals(<paramref name="key"/>, <paramref name="value"/>) must always evaluate to true</param>
        /// <param name="mapValue">The value used in <see cref="VectorFlatHashMap{K,V}"/></param>
        /// <param name="value">The initial element to insert, when <paramref name="key"/> is not present</param>
        /// <param name="mergerFunc">The merge function used to merge the current stored value with the new <paramref name="mapValue"/></param>
        /// <param name="ctor">The constructor function used to construct a <typeparamref name="T"/> value from the <paramref name="mergerFunc"/> result</param>
        /// <param name="extractor">The opposite of <paramref name="ctor"/>. Used to extract a <typeparamref name="V"/> value from the currently stored <typeparamref name="T"/> value</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        protected ref T ComputeMerge<U, V>(U mapKey, T key, V mapValue, T value, [NotNull] Func<V, V, V> mergerFunc,
            [NotNull] Func<U, V, T> ctor, [NotNull] Func<T, V> extractor) {
            if (_arrVal is null)
                Initialize();
            else if (size > LOAD_FACTOR * _capacity)
                Rehash();

            var cmp = _cmp;


            var arr = _arrVal!;
            var metHash = _lookupMetadata!;
            var metDist = _distanceMetadata!;

            uint len = _capacity;
            uint cap = len - 1;

            uint idx = HashSlot(key is null ? 0 : cmp.GetHashCode(key), len, out byte meta);

            byte distance = 0;

            for (uint i = 0; i < len; ++i, idx = (idx + 1) & cap) {
                byte sHash = metHash![idx];
                if (sHash == 0) {
                    arr![idx] = value;
                    metHash[idx] = meta;
                    metDist![idx] = distance;
                    ++size;
                    return ref arr[idx];
                }

                if (sHash == meta && cmp.Equals(key, arr![idx])) {
                    arr[idx] = ctor(mapKey, mergerFunc(extractor(arr[idx]), mapValue));
                    return ref arr[idx];
                }

                if (metDist![idx] < distance) {
                    // robin hood
                    var insKy = value;
                    (arr![idx], key) = (insKy, arr[idx]);
                    (meta, metHash[idx]) = (metHash[idx], meta);
                    (distance, metDist[idx]) = (metDist[idx], distance);
                    //InsertInternal(key, false, idx, meta, distance);
                    InsertUnique(key, idx, meta, distance);
                    return ref _arrVal == arr ? ref arr[idx] : ref GetOrElse(insKy, ref arr[idx]);
                }

                checked {
                    ++distance;
                }
            }

            // unreachable
            return ref arr![0];
        }

        /// <summary>
        /// Performs a hashtable lookup on <paramref name="key"/> and returns true, iff an element equal to <paramref name="key"/> is contained in the hashtable
        /// </summary>
        protected bool ContainsInternal(T key) {
            return TryGetIndex(key, out _);
        }

        /// <summary>
        /// Performs a hashtable lookup on <paramref name="key"/> and returns a reference to the stored key if it is contained. Otherwise, returns the <paramref name="orElse"/> reference
        /// </summary>
        /// <param name="key">The key to lookup</param>
        /// <param name="orElse">The fallback reference to return, if the <paramref name="key"/> is not contained</param>
        /// <param name="succ">True, iff an item equal to <paramref name="key"/> is contained</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected ref T GetOrElse(T key, ref T orElse, out bool succ) {
            if (!TryGetIndex(key, out uint idx)) {
                succ = false;
                return ref orElse;
            }

            succ = true;
            return ref _arrVal![idx];
        }

        /// <summary>
        /// Same as <see cref="GetOrElse(T,ref T,out bool)"/>
        /// </summary>
        protected ref T GetOrElse(T key, ref T orElse) {
            return ref !TryGetIndex(key, out uint idx) ? ref orElse : ref _arrVal![idx];
        }

        /// <summary>
        /// Tries to insert the <paramref name="key"/> into the hashtable. Iff <paramref name="canReplace"/> is true, an equal element, which is already
        /// contained in the hashtable will be replaced by the new <paramref name="key"/>. Otherwise the insertion will fail in such a case and return false.
        /// </summary>
        /// <param name="key">The element to insert</param>
        /// <param name="canReplace">True to replace the already contained <paramref name="key"/></param>
        /// <returns>True, iff the <paramref name="key"/> was inserted</returns>
        protected bool Insert(T key, bool canReplace) {
            if (_arrVal is null)
                Initialize();
            else if (size > LOAD_FACTOR * _capacity)
                Rehash();

            //return _capacity >= 32 ? InsertInternalSimd(key, canReplace) : InsertInternal(key, canReplace);
            return InsertInternal(key, canReplace);
        }

        /// <summary>
        /// Tries to insert the <paramref name="key"/> into the hashtable and returns a reference to the newly inserted element. If an element equal to <paramref name="key"/> is
        /// already contained in the hashtable, no insertion takes place and a reference to the old value is returned instead.
        /// </summary>
        /// <param name="key">The element to insert</param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        protected ref T InsertIfAbsent(T key) {
            if (_arrVal is null)
                Initialize();
            else if (size > LOAD_FACTOR * _capacity)
                Rehash();

            var cmp = _cmp;


            var arr = _arrVal!;
            var metHash = _lookupMetadata!;
            var metDist = _distanceMetadata!;

            uint len = _capacity;
            uint cap = len - 1;
            uint idx = HashSlot(key is null ? 0 : cmp.GetHashCode(key), len, out byte meta);

            byte distance = 0;

            for (uint i = 0; i < len; ++i, idx = (idx + 1) & cap) {
                byte sHash = metHash![idx];
                if (sHash == 0) {
                    arr![idx] = key;
                    metHash[idx] = meta;
                    metDist![idx] = distance;
                    ++size;
                    return ref arr[idx];
                }

                if (sHash == meta && cmp.Equals(key, arr![idx]))
                    return ref arr[idx];

                if (metDist![idx] < distance) {
                    // robin hood
                    var insKy = key;
                    (arr![idx], key) = (insKy, arr[idx]);
                    (meta, metHash[idx]) = (metHash[idx], meta);
                    (distance, metDist[idx]) = (metDist[idx], distance);
                    //InsertInternal(key, false, idx, meta, distance);
                    InsertUnique(key, idx, meta, distance);
                    return ref _arrVal == arr ? ref arr[idx] : ref GetOrElse(insKy, ref arr[idx]);
                }

                checked {
                    ++distance;
                }
            }

            // unreachable
            return ref arr![0];
        }

        /// <summary>
        /// Inserts the <paramref name="key"/> into the hashtable. It is already known, that <paramref name="key"/> is not already contained,
        /// that there is at least one free slot and that SSE2 vector instructions are supported
        /// </summary>
        /// <param name="key">The element to insert</param>
        /// <param name="idx">The index in <see cref="_arrVal"/> where to start probing</param>
        /// <param name="meta">The 7 most significant bits of the hashcode*MAGIC_NUMBER OR 128 of <paramref name="key"/> </param>
        /// <param name="distance">The length of the probing sequence from the optimal slot for <paramref name="key"/> to <paramref name="idx"/></param>
        /// <param name="metDist">A pointer to the first elemenet of the <see cref="_distanceMetadata"/> array</param>
        /// <param name="metHash">A pointer to the first elemenet of the <see cref="_lookupMetadata"/> array</param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public unsafe void InsertUniqueUnsafe(T key, uint idx, byte meta, byte distance, byte* metDist, byte* metHash) {
            uint len = _capacity;
            // assume, we wont need to rehash from here

            var arr = _arrVal!;

            uint cap = len - 1;
            var vec = Sse.LoadVector128(metHash + idx - distance);
            int mask = Sse.MoveMask(Sse.CompareEqual(vec, Simd.Create((byte) 0)));
            uint i = idx - distance + mask.Current();
            if (mask != 0 && i < len) {
                arr![i] = key;
                metHash[i] = meta;
                metDist![i] = distance;

                ++size;
                return;
            }

            do {
                byte dist = metDist![idx];

                if (dist == 0 && metHash![idx] == 0) {
                    arr![idx] = key;
                    metHash[idx] = meta;
                    metDist![idx] = distance;

                    ++size;
                    return;
                }

                if (dist < distance) {
                    // robin hood
                    Swap(ref arr![idx], ref key);
                    Swap(ref meta, ref metHash![idx]);
                    Swap(ref distance, ref metDist[idx]);
                }

                checked {
                    ++distance;
                }

                idx = (idx + 1) & cap;
            } while (true);
        }

        /// <summary>
        /// Inserts the <paramref name="key"/> into the hashtable. It is already known, that <paramref name="key"/> is not already contained and that there is at least one free slot.
        ///
        /// This method is called from <see cref="Rehash(uint)"/>
        /// </summary>
        /// <param name="key">The element to reinsert</param>
        /// <param name="idx">The index, where to start probing</param>
        /// <param name="meta">The 7 most significant bits of the hashcode*MAGIC_NUMBER OR 128 of <paramref name="key"/></param>
        /// <param name="distance">The length of the probing sequence from the optimal slot for <paramref name="key"/> to <paramref name="idx"/></param>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private unsafe void InsertUnique(T key, uint idx, byte meta, byte distance) {
            uint len = _capacity;
            // assume, we wont need to rehash from here

            var arr = _arrVal!;

            // elide range check
            fixed (byte* metDist = _distanceMetadata!, metHash = _lookupMetadata!) {
                uint cap = len - 1;

                do {
                    byte dist = metDist![idx];

                    if (dist == 0 && metHash![idx] == 0) {
                        arr![idx] = key;
                        metHash[idx] = meta;
                        metDist![idx] = distance;

                        ++size;
                        return;
                    }

                    if (dist < distance) {
                        // robin hood
                        Swap(ref arr![idx], ref key);
                        Swap(ref meta, ref metHash![idx]);
                        Swap(ref distance, ref metDist[idx]);
                    }

                    checked {
                        ++distance;
                    }

                    idx = (idx + 1) & cap;
                } while (true);
            }
        }

        /// <summary>
        /// Swaps the values from the two given references
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        private static void Swap<U>(ref U x, ref U y) {
            var tmp = x;
            x = y;
            y = tmp;
            //(y, x) = (x, y);
        }

        /// <summary>
        /// Tries to insert the given <paramref name="key"/> into the hashtable. It is already known, that there is at least one free slot left
        /// </summary>
        /// <param name="key">The item to insert</param>
        /// <param name="canReplace">True to replace the already contained <paramref name="key"/></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        protected unsafe bool InsertInternal(T key, bool canReplace) {
            var cmp = _cmp;
            uint len = _capacity;
            // assume, we wont need to rehash from here

            var arr = _arrVal!;
            fixed (byte* metDist = _distanceMetadata!, metHash = _lookupMetadata) {
                uint cap = len - 1;
                uint idx = HashSlot(key is null ? 0 : cmp.GetHashCode(key), _capacity, out byte meta);
                byte distance = 0;
                //var numDistMask = numDist!.ULength() - 1;
                if (Sse.IsSupported) {
                    var vec = Sse.LoadVector128(metHash + idx);
                    int mask = Sse.MoveMask(Sse.CompareEqual(vec, Simd.Create(meta)));
                    while (mask != 0) {
                        uint i = idx + mask.Next();
                        if (cmp.Equals(arr![i], key)) {
                            if (!canReplace)
                                return false;

                            arr[i] = key;
                            metHash![i] = meta;
                            metDist![i] = distance;
                            return true;
                        }
                    }

                    uint end = Math.Min(VEC_LEN, len - idx);
                    for (uint i = 0; i < end; ++i) {
                        byte sHash = metHash![idx];
                        if (sHash == 0) {
                            arr![idx] = key;
                            metHash[idx] = meta;
                            metDist![idx] = distance;

                            ++size;
                            return true;
                        }

                        byte dist = metDist![idx];
                        if (dist < distance) {
                            // robin hood
                            //(arr![x], key) = (key, arr[x]);
                            Swap(ref arr![idx], ref key);
                            //(meta, metHash![x]) = (metHash[x], meta);
                            Swap(ref meta, ref metHash![idx]);
                            //(distance, ptr[x]) = (d, distance);
                            Swap(ref distance, ref metDist[idx]);

                            InsertUniqueUnsafe(key, idx, meta, distance, metDist, metHash);
                            return true;
                        }

                        // can be unchecked here, since 0 + 16 does not overflow
                        ++distance;
                        idx = (idx + 1) & cap;
                    }
                }

                do {
                    byte sHash = metHash![idx];
                    if (sHash == 0) {
                        arr![idx] = key;
                        metHash[idx] = meta;
                        metDist![idx] = distance;

                        ++size;
                        return true;
                    }

                    byte dist = metDist![idx];
                    if (dist < distance) {
                        // robin hood
                        //(arr![x], key) = (key, arr[x]);
                        Swap(ref arr![idx], ref key);
                        //(meta, metHash![x]) = (metHash[x], meta);
                        Swap(ref meta, ref metHash![idx]);
                        //(distance, ptr[x]) = (d, distance);
                        Swap(ref distance, ref metDist[idx]);

                        if (Sse.IsSupported)
                            InsertUniqueUnsafe(key, idx, meta, distance, metDist, metHash);
                        else
                            InsertUnique(key, idx, meta, distance);

                        return true;
                    }

                    // if equals, then metDist[idx] == distance
                    if (sHash == meta && dist == distance && cmp.Equals(key, arr![idx])) {
                        if (!canReplace)
                            return false;

                        arr[idx] = key;
                        metHash[idx] = meta;
                        metDist![idx] = distance;
                        return true;
                    }

                    checked {
                        ++distance;
                    }

                    idx = (idx + 1) & cap;
                } while (true);
            }

            // unreachable
            // return false;
        }

        /// <summary>
        /// Removes the element at the slot with the index <paramref name="idx"/> and rearranges the following elements to prevent tombstones/sentinels
        /// </summary>
        /// <param name="idx">The index, where to remove</param>
        /// <returns>True</returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        protected bool RemoveAt(uint idx) {
            var metHash = _lookupMetadata!;
            var metDist = _distanceMetadata!;
            var arr = _arrVal!;
            uint len = _capacity;
            uint cap = len - 1;

            if (--size == 0) {
                metHash![idx] = 0;
                metDist![idx] = 0;
                arr![idx] = default!;
                return true;
            }

            do {
                uint i = idx;
                idx = (idx + 1) & cap;
                if (metDist![idx] == 0) {
                    metHash![i] = 0;
                    metDist[i] = 0;
                    arr![i] = default!;
                    return true;
                }

                metHash![i] = metHash[idx];
                metDist[i] = unchecked((byte) (metDist[idx] - 1));
                arr![i] = arr[idx];
            } while (true);
        }
        /// <summary>
        /// Removes the element equal to <paramref name="key"/> from the hashtable, if it is contained.
        /// </summary>
        /// <param name="key">The element to remove</param>
        /// <returns>True, if <paramref name="key"/> was actually removed &lt;=&gt; <see cref="ContainsInternal"/> would have returned true before</returns>
        protected bool RemoveInternal(T key) {
            return TryGetIndex(key, out uint idx) && RemoveAt(idx);
        }
        /// <summary>
        /// Performs a hashtable lookup to search for <paramref name="key"/>.
        ///
        /// Should not be made publicly accessible.
        /// </summary>
        /// <param name="key">The element to search for</param>
        /// <param name="index">The index of <paramref name="key"/> in <see cref="_arrVal"/> if it is contained</param>
        /// <returns>True, iff <paramref name="key"/> is contained</returns>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        protected bool TryGetIndex(T key, out uint index) {
            var arr = _arrVal;
            if (arr is null || size == 0) {
                index = ~0u;
                return false;
            }

            var cmp = _cmp;

            uint len = arr.ULength();
            uint idx = HashSlot(key == null ? 0 : cmp.GetHashCode(key), len, out byte meta);


            var metHash = _lookupMetadata!;

            if (len > 8 && Sse.IsSupported) {
                var hashVec = Simd.Create(meta);
                unsafe {
                    fixed (byte* ptr = metHash) {
                        for (uint i = idx; i < len; i += VEC_LEN) {
                            var vec = Sse.LoadVector128(ptr + i);
                            var cmpVec = Sse.CompareEqual(vec, hashVec);
                            int mask = Sse.MoveMask(cmpVec);
                            while (mask.Has()) {
                                uint x = mask.Next() + i;

                                if (cmp.Equals(key, arr[x])) {
                                    index = i;
                                    return true;
                                }
                            }

                            if (Sse.MoveMask(Sse.CompareEqual(SimdVector.Zero, vec)) != 0) {
                                index = ~0u;
                                return false;
                            }
                        }

                        for (uint i = 0; i < idx; i += VEC_LEN) {
                            var vec = Sse.LoadVector128(ptr + i);
                            var cmpVec = Sse.CompareEqual(vec, hashVec);
                            int mask = Sse.MoveMask(cmpVec);
                            while (mask.Has()) {
                                uint x = mask.Next() + i;
                                if (cmp.Equals(key, arr[x])) {
                                    index = x;
                                    return true;
                                }
                            }

                            if (Sse.MoveMask(Sse.CompareEqual(SimdVector.Zero, vec)) != 0) {
                                index = ~0u;
                                return false;
                            }
                        }
                    }
                }
            }
            else {
                for (uint i = idx; i < len; ++i)
                    if (metHash![i] == meta && cmp.Equals(key, arr[i])) {
                        index = i;
                        return true;
                    }

                for (uint i = 0; i < idx; ++i)
                    if (metHash![i] == meta && cmp.Equals(key, arr[i])) {
                        index = i;
                        return true;
                    }
            }

            index = ~0u;
            return false;
        }

        /// <summary>
        /// Initializes the internal structures with an initial capacity of <paramref name="cap"/>
        /// </summary>
        /// <param name="cap">The initial capacity</param>
        private void Initialize(uint cap = 8) {
            uint len = cap;
            _arrVal = new T[len];
            _distanceMetadata = new byte[len];
            // allocate more to simplify SIMD usage
            _lookupMetadata = new byte[len + Offset];
            _capacity = cap;
            size = 0;
        }
        /// <summary>
        /// Reallocates the internal structures to a new capacity <paramref name="minCap"/> and reinserts all contained elements
        /// </summary>
        private void Rehash(uint minCap) {
            var metHash = _lookupMetadata!;
            var arr = _arrVal!;

            uint len = arr.ULength();

            _capacity = minCap;
            _arrVal = new T[minCap];
            _distanceMetadata = new byte[minCap];
            _lookupMetadata = new byte[minCap + Offset];
            //numDist = new uint[BitHelper.NextPow2(BitHelper.Log2(minCap))];

            var cmp = _cmp;

            size = 0;

            if (len >= 32 && Avx2.IsSupported) {
                unsafe {
                    fixed (byte* ptr = metHash) {
                        for (uint i = 0; i < len; i += 32) {
                            var vec = Avx.LoadVector256(ptr + i);
                            int mask = ~Avx2.MoveMask(Avx2.CompareEqual(vec, Vector256<byte>.Zero));
                            while (mask.Has()) {
                                uint x = mask.Next() + i;
                                var key = arr[x];
                                uint idx = HashSlot(key is null ? 0 : cmp.GetHashCode(key), minCap, out byte meta);
                                InsertUnique(key, idx, meta, 0);
                                //InsertInternal(key, false, idx, meta, 0);
                                //Insert(arr[x], false);
                            }
                        }
                    }
                }
            }
            else if (Sse.IsSupported) {
                unsafe {
                    fixed (byte* ptr = metHash) {
                        for (uint i = 0; i < len; i += VEC_LEN) {
                            var vec = Sse.LoadVector128(ptr + i);
                            int mask = ~Sse.MoveMask(Sse.CompareEqual(vec, SimdVector.Zero)) & ushort.MaxValue;
                            while (mask.Has()) {
                                uint x = mask.Next() + i;
                                var key = arr[x];
                                uint idx = HashSlot(key is null ? 0 : cmp.GetHashCode(key), minCap, out byte meta);
                                InsertUnique(key, idx, meta, 0);
                                //InsertInternal(key, false, idx, meta, 0);
                                //Insert(arr[x], false);
                            }
                        }
                    }
                }
            }
            else {
                for (uint i = 0; i < len; ++i)
                    if (metHash![i] != 0) {
                        var key = arr[i];
                        uint idx = HashSlot(key is null ? 0 : cmp.GetHashCode(key), minCap, out byte meta);
                        InsertUnique(key, idx, meta, 0);
                    }
            }
        }
        /// <summary>
        /// Reallocates the internal structures to the doubled capacity and reinserts all contained elements
        /// </summary>
        private void Rehash() {
            Rehash(_capacity << 1);
        }

        /// <summary>
        /// An enumerator, which can be used to iterate through the hashtable. However, if all elements should be iterated in a loop, prefer
        /// <see cref="VectorFlatHashTable{T}.ForEach(System.Action{T})"/> or <see cref="VectorFlatHashTable{T}.ForEach(System.Func{T,bool})"/>
        /// over iterators for performance reasons
        /// </summary>
        public struct Iterator : IEnumerator<T> {
            private JIterator jit;

            private T curr;

            public Iterator(VectorFlatHashTable<T> ht) {
                jit = new JIterator(ht);
                curr = default!;
            }

            public bool MoveNext() {
                if (jit.HasNext()) {
                    curr = jit.Next();
                    return true;
                }

                return false;
            }

            /// <inheritdoc />
            public void Reset() {
                jit.Reset();
            }

            /// <inheritdoc />
            object? IEnumerator.Current => Current;

            public T Current => curr;

            /// <inheritdoc />
            public void Dispose() {
                jit = default;
            }
        }
        /// <summary>
        /// An enumerator, which can be used to iterate through the hashtable and automatically map the elements to a new format. However, if all elements should be iterated in a loop, prefer
        /// <see cref="VectorFlatHashTable{T}.ForEach(System.Action{T})"/> or <see cref="VectorFlatHashTable{T}.ForEach(System.Func{T,bool})"/>
        /// over iterators for performance reasons
        /// </summary>
        public struct MapIterator<U> : IEnumerator<U> {
            private Iterator it;
            private readonly Func<T, U> mapper;

            public MapIterator(VectorFlatHashTable<T> ht, Func<T, U> mapper) {
                it = new Iterator(ht);
                this.mapper = mapper;
            }

            /// <inheritdoc />
            public bool MoveNext() {
                return it.MoveNext();
            }

            /// <inheritdoc />
            public void Reset() {
                it.Reset();
            }

            /// <inheritdoc />
            public U Current => mapper(it.Current);

            /// <inheritdoc />
            object? IEnumerator.Current => Current;

            /// <inheritdoc />
            public void Dispose() {
                it.Dispose();
            }
        }
        /// <summary>
        /// Convenience class, which allows constructing instances of <see cref="MapIterator{U}"/> without the need to specify the
        /// type parameter explicitly
        /// </summary>
        public static class MapIterator {
            /// <summary>
            /// Constructs a new <see cref="MapIterator{U}"/> with the given properties
            /// </summary>
            public static MapIterator<U> Create<U>(VectorFlatHashTable<T> ht, Func<T, U> mapper) {
                return new MapIterator<U>(ht, mapper);
            }
        }
        /// <summary>
        /// An Java-like iterator, which can be used to iterate through the hashtable. However, if all elements should be iterated in a loop, prefer
        /// <see cref="VectorFlatHashTable{T}.ForEach(System.Action{T})"/> or <see cref="VectorFlatHashTable{T}.ForEach(System.Func{T,bool})"/>
        /// over iterators for performance reasons
        /// </summary>
        public struct JIterator {
            private readonly T[]? _arrVal;
            private readonly uint _len;
            private readonly byte[]? _lookupMetadata;
            private uint _idx;

            public JIterator(VectorFlatHashTable<T> ht) {
                _arrVal = ht._arrVal;
                _lookupMetadata = ht._lookupMetadata;
                _idx = 0;
                if (_arrVal != null) {
                    _len = _arrVal.ULength();
                    NextIndex();
                }
                else
                    _len = 0;
            }

            public void Reset() {
                _idx = 0;
                if (_arrVal != null)
                    NextIndex();
            }

            public bool HasNext() {
                return _idx < _len;
            }

            public ref T Next() {
                ref var ret = ref _arrVal![_idx];
                ++_idx;
                NextIndex();
                return ref ret;
            }

            private void NextIndex() {
                var metHash = _lookupMetadata;
                while (_idx < _len && metHash![_idx] == 0)
                    _idx++;
            }
        }
    }

    /// <summary>
    /// The base class of all generic instantiations of <see cref="VectorFlatHashTable{T}"/>. Holds static constants and provides some utility
    /// functionality which is independent from any type parameter
    /// </summary>
    public class VectorFlatHashTableBase {
        protected internal const float LOAD_FACTOR = 0.875f;
        protected internal const uint MAGIC_NUMBER = 2_654_435_769;
        protected internal const uint VEC_LEN = 16;
        protected internal static readonly uint Offset = Sse.IsSupported ? 16u : 0u;

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        internal static uint HashSlot(int hc, uint cap, out byte meta) {
            unchecked {
                uint x = (uint) hc * MAGIC_NUMBER;
                meta = (byte) ((x >> 25) | 0b10000000);
                return x & (cap - 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static byte HashMetadata(int hc) {
            return (byte) ((((uint) hc * MAGIC_NUMBER) >> 25) | 0b10000000);
        }

        private protected VectorFlatHashTableBase() { }
    }
}