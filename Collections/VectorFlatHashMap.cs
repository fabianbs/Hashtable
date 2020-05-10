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
using System.Linq;

namespace Collections {
    /// <summary>
    /// A hash map, which is built on top of <see cref="VectorFlatHashTable{T}"/>
    /// </summary>
    /// <typeparam name="K">The type of the keys</typeparam>
    /// <typeparam name="V">The type of the values</typeparam>
    public class VectorFlatHashMap<K, V> : VectorFlatHashTable<KVPair<K, V>>, IDictionary<K, V> where K : notnull {
        private static KVPair<K, V> _dflt;

        /// <summary>
        /// The number of key-value pairs stored in this map
        /// </summary>
        public uint Size => size;
        /// <summary>
        /// The number of key-value pairs stored in this map
        /// </summary>
        public int Count => (int) size;
        /// <summary>
        /// This map is not readonly
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// A collection containing all keys of this map.
        ///
        /// <remarks>Optimize this</remarks>
        /// </summary>
        public ICollection<K> Keys => this.Select(x => x.Key).AsCollection(Count);
        /// <summary>
        /// A collection containing all values of this map.
        ///
        /// <remarks>Optimize this</remarks>
        /// </summary>
        public ICollection<V> Values => this.Select(x => x.Value).AsCollection(Count);

        /// <summary>
        /// Default constructor. Does not allocate any memory (apart from retrieving the default equality comparer)
        /// </summary>
        public VectorFlatHashMap() { }
        /// <summary>
        /// Constructor. Does not allocate any memory.
        /// </summary>
        /// <param name="cmp"></param>
        public VectorFlatHashMap(IEqualityComparer<K> cmp)
            : base(KVPair<K, V>.GetComparer(cmp)) {
        }
        /// <summary>
        /// Constructor. Initializes the map with an initial capacity and the default equality comparer
        /// </summary>
        /// <param name="initialCapacity">A lower bound to the initial capacity of this map</param>
        public VectorFlatHashMap(uint initialCapacity)
            : base(initialCapacity) {
        }
        /// <summary>
        /// Constructor. Initializes the map with an initial capacity and the given equality comparer <paramref name="cmp"/>
        /// </summary>
        /// <param name="initialCapacity">A lower bound to the initial capacity of this map</param>
        /// <param name="cmp">The euqlity comparer used to compare keys and to retrieve their hashcodes</param>
        public VectorFlatHashMap(uint initialCapacity, IEqualityComparer<K> cmp)
            : base(initialCapacity, KVPair<K, V>.GetComparer(cmp)) {
        }
        /// <inheritdoc />
        public V this[K key] {
            get {
                var ret = GetOrElse(new KVPair<K, V>(key), ref _dflt, out bool succ);
                if (!succ)
                    throw new KeyNotFoundException();

                return ret.Value;
            }
            set => Insert(new KVPair<K, V>(key, value), true);
        }

        /// <summary>
        /// Tries to insert the key-value pair with the <paramref name="key"/> as key and <paramref name="value"/> as value.
        /// If there is already a key-value pair with an equal key, the map remains unchanged
        /// </summary>
        /// <param name="key">The key to insert</param>
        /// <param name="value">The value to insert</param>
        /// <returns>A reference to the value associated with <paramref name="key"/> in the map</returns>
        public ref V GetOrAdd(K key, V value) {
            return ref InsertIfAbsent(new KVPair<K, V>(key, value)).Value;
        }

        /// <inheritdoc />
        void IDictionary<K, V>.Add(K key, V value) {
            Add(key, value);
        }
        /// <inheritdoc />
        void ICollection<KeyValuePair<K, V>>.Add(KeyValuePair<K, V> item) {
            var (key, value) = item;
            Add(key, value);
        }
        /// <inheritdoc />
        public void Clear() {
            ClearInternal();
        }
        /// <inheritdoc />
        public bool Contains(KeyValuePair<K, V> item) {
            var (key, value) = item;
            ref var ret = ref GetOrElse(new KVPair<K, V>(key), ref _dflt, out bool succ);
            return succ && Equals(ret.Value, value);
        }
        /// <inheritdoc />
        public bool ContainsKey(K key) {
            return ContainsInternal(new KVPair<K, V>(key));
        }
        /// <inheritdoc />
        /// <remarks>Optimize this</remarks>
        public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex) {
            var it = new JIterator(this);

            while (it.HasNext() && arrayIndex < array.Length) {
                ref var nxt = ref it.Next();
                array[arrayIndex] = new KeyValuePair<K, V>(nxt.Key, nxt.Value);
            }
        }
        /// <inheritdoc />
        /// <remarks>Use <see cref="VectorFlatHashTable{T}.Iterator"/> instead</remarks>
        public IEnumerator<KeyValuePair<K, V>> GetEnumerator() {
            var it = new JIterator(this);

            while (it.HasNext()) {
                var kvp = it.Next();
                yield return new KeyValuePair<K, V>(kvp.Key, kvp.Value);
            }
        }
        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
        /// <inheritdoc />
        public bool Remove(K key) {
            return RemoveInternal(new KVPair<K, V>(key));
        }
        /// <inheritdoc />
        public bool Remove(KeyValuePair<K, V> item) {
            return TryGetIndex(new KVPair<K, V>(item.Key), out uint idx) && RemoveAt(idx);
        }
        /// <inheritdoc />
        public bool TryGetValue(K key, [MaybeNullWhen(false)] out V value) {
           
            if (TryGetIndex(new KVPair<K, V>(key), out uint idx)) {
                value = this[idx].Value;
                return true;
            }

            value = default;
            return false;
        }
        /// <summary>
        /// Tries to insert a new key-value pair (<paramref name="key"/>, <paramref name="value"/>) into this map.
        /// If this map already contains a key-value pair, where the key equals the <paramref name="key"/> to insert, the map remains unchanged
        /// </summary>
        /// <param name="key">The key to insert</param>
        /// <param name="value">The value to insert</param>
        /// <returns>True, iff the key-value pair was actually inserted</returns>
        public bool Add(K key, V value) {
            return Insert(new KVPair<K, V>(key, value), false);
        }

        /// <summary>
        /// Tries to lookup a new key-value pair into this map, where the key is <paramref name="key"/> and the value
        /// is computed by <paramref name="mapper"/>(<paramref name="key"/>). If this map already contains a key-value pair with a key
        /// equal to the <paramref name="key"/> to insert, the map remains unchanged and the <paramref name="mapper"/> is not executed.
        ///
        /// Compare to the <c>computeIfAbsent</c> function from the java.utils.Map interface
        /// </summary>
        /// <param name="key">The key to insert</param>
        /// <param name="mapper">A constructor to create a <typeparamref name="V"/> value to be associated with <paramref name="key"/>.
        /// Note: Do not capture the key in <paramref name="mapper"/> for performance reasons. Use the parameter instead</param>
        /// <returns>A reference to the value associated with <paramref name="key"/> in this map</returns>
        public ref V ComputeIfAbsent(K key, Func<K, V> mapper) {
            static KVPair<K, V> Ctor(K k, V v) {
                return new KVPair<K, V>(k, v);
            }

            return ref ComputeIfAbsent(key, new KVPair<K, V>(key), mapper, Ctor).Value;
        }

        /// <summary>
        /// Searches for a key-value pair with the key <paramref name="key"/> in this map. If such a pair was found, the <paramref name="reMapper"/>
        /// function is used to compute the new value, which will replace the old value <c>oldValue</c>. The new key-value pair will be
        /// (<paramref name="key"/>, <paramref name="reMapper"/>(<paramref name="key"/>, <c>oldValue</c>)).
        /// If the lookup did not find any such key-value pair, the map remains unchanged.
        ///
        /// Compare to the <c>computeIfPresent</c> function from the java.utils.Map interface
        /// </summary>
        /// <param name="key"></param>
        /// <param name="reMapper"></param>
        /// <returns></returns>
        public bool ComputeIfPresent(K key, Func<K, V, V> reMapper) {
            if (TryGetIndex(new KVPair<K, V>(key), out uint idx)) {
                this[idx].Value = reMapper(key, this[idx].Value);
                return true;
            }

            return false;
        }
        /// <summary>
        /// Performs a lookup on <paramref name="key"/> and returns a reference to the value associated to <paramref name="key"/> if it is contained.
        /// Otherwise, returns the <paramref name="orElse"/> reference.
        /// </summary>
        /// <param name="key">The key to search for</param>
        /// <param name="orElse">The fallback reference</param>
        public ref V GetOrElse(K key, ref V orElse) {
            if (TryGetIndex(new KVPair<K, V>(key), out uint idx))
                return ref this[idx].Value;

            return ref orElse;
        }

        /// <summary>
        /// Searches for a key-value pair with the key <paramref name="key"/> in this map. If no such pair was found,
        /// insert the pair (<paramref name="key"/>, <paramref name="singleValue"/>). Otherwise replace the old associated value <c>oldValue</c> by
        /// <paramref name="mergeFunc"/>(<c>oldValue</c>, <paramref name="singleValue"/>).
        ///
        /// Compare to the <c>merge</c> function from the java.utils.Map interface
        /// </summary>
        /// <param name="key">The key to search for (or to insert</param>
        /// <param name="singleValue">The initial associated value</param>
        /// <param name="mergeFunc">A merge function to combine the currently to <paramref name="key"/> associated value with
        /// <paramref name="singleValue"/> to a new associated value</param>
        /// <returns>A reference to the (new) value associated to <paramref name="key"/></returns>
        public ref V Merge(K key, V singleValue, Func<V, V, V> mergeFunc) {
            static KVPair<K, V> Ctor(K k, V v) {
                return new KVPair<K, V>(k, v);
            }

            static V Extractor(KVPair<K, V> kvp) {
                return kvp.Value;
            }

            return ref ComputeMerge(key, new KVPair<K, V>(key), singleValue, new KVPair<K, V>(key, singleValue),
                mergeFunc, Ctor, Extractor).Value;
        }

        /// <summary>
        /// Searches for a key-value pair with tha key <paramref name="key"/> in this map. If such a pair was found,
        /// replaces the associated value by <paramref name="nwVal"/>. Otherwise, the map remains unchanged.
        /// </summary>
        /// <param name="key">The key to search for</param>
        /// <param name="nwVal">The value used to replace the old associated value</param>
        /// <returns>True, if the value was replaced</returns>
        public bool Replace(K key, V nwVal) {
            if (TryGetIndex(new KVPair<K, V>(key), out uint idx)) {
                base[idx].Value = nwVal;
                return false;
            }

            return false;
        }
        /// <summary>
        /// Searches for a key-value pair with tha key <paramref name="key"/> in this map. If such a pair was found and the
        /// associated value equals <paramref name="oldVal"/>, replaces it by <paramref name="nwVal"/>.
        /// Otherwise, the map remains unchanged.
        /// </summary>
        /// <param name="key">The key to search for</param>
        /// <param name="oldVal">The value to search for</param>
        /// <param name="nwVal">The value used to replace the old associated value</param>
        /// <returns>True, if the value was replaced</returns>
        public bool Replace(K key, V oldVal, V nwVal) {
            if (TryGetIndex(new KVPair<K, V>(key), out uint idx) && Equals(oldVal, this[idx])) {
                base[idx].Value = nwVal;
                return false;
            }

            return false;
        }

        /// <summary>
        /// Adds all key-value pairs from the given map <paramref name="values"/> into this map skipping all pairs,
        /// where an equal key is already present in this map.
        /// </summary>
        /// <param name="values">The map to insert into this map</param>
        /// <returns>The number of key-value pairs, which were actually inserted</returns>
        public uint AddAll(IDictionary<K, V> values) {
            Reserve(values.UCount());
            uint ctr = 0;
            foreach (var (key, value) in values) {
                if (Insert(new KVPair<K, V>(key, value), false))
                    ctr++;
            }

            return ctr;
        }
    }
}