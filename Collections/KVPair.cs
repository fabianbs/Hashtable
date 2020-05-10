//  ******************************************************************************
//  * Copyright (c) 2020 Fabian Schiebel.
//  * All rights reserved. This program and the accompanying materials are made
//  * available under the terms of LICENSE.txt.
//  *
//  *****************************************************************************/
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Collections {
    /// <summary>
    /// A key-value pair, which equality and hashcode is only based on the key
    /// </summary>
    /// <typeparam name="K">The key-tape</typeparam>
    /// <typeparam name="V">The value-type</typeparam>
    public struct KVPair<K, V> : IEquatable<KVPair<K, V>> {
        /// <summary>
        /// The key. Equality and hashcode is only based on the key
        /// </summary>
        public readonly K Key;
        /// <summary>
        /// The value associated to <see cref="Key"/>
        /// </summary>
        public V Value;

        /// <summary>
        /// Constructor
        /// </summary>
        public KVPair(K ky, V val) {
            Key = ky;
            Value = val;
        }
        /// <summary>
        /// Constructor. Initializes the value to <see langword="default"/>(<typeparamref name="V"/>)
        /// </summary>
        public KVPair(K ky) {
            Key = ky;
            Value = default!;
        }
        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is KVPair<K, V> pair && Equals(pair);
        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(KVPair<K, V> other) => EqualityComparer<K>.Default.Equals(Key, other.Key);
        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => HashCode.Combine(Key);

        public static bool operator ==(KVPair<K, V> left, KVPair<K, V> right) => left.Equals(right);
        public static bool operator !=(KVPair<K, V> left, KVPair<K, V> right) => !(left == right);
        
        public override string ToString() => $"Key: {Key}, Value: {Value}";

        /// <summary>
        /// A <see cref="IEqualityComparer{T}"/> to compare two instance of <see cref="KVPair{K,V}"/>. The equality is only based on
        /// the keys and is completely independent of the associated values
        /// </summary>
        readonly struct KVPairComparer : IEqualityComparer<KVPair<K, V>> {
            private readonly IEqualityComparer<K> kyCmp;
            public KVPairComparer(IEqualityComparer<K> kyCmp) => this.kyCmp = kyCmp;

            /// <inheritdoc />
            public bool Equals(KVPair<K, V> x, KVPair<K, V> y) {
                return kyCmp.Equals(x.Key, y.Key);
            }

            /// <inheritdoc />
            public int GetHashCode(KVPair<K, V> obj) {
                return obj.Key is null ? 0 : kyCmp.GetHashCode(obj.Key);
            }
        }
        /// <summary>
        /// Retrieve a <see cref="IEqualityComparer{T}"/> for caomparing <see cref="KVPair{K,V}"/> instances
        /// </summary>
        public static IEqualityComparer<KVPair<K, V>> GetComparer(IEqualityComparer<K>? kyCmp = null) {
            if (kyCmp is null)
                return EqualityComparer<KVPair<K, V>>.Default;

            return new KVPairComparer(kyCmp);
        }
    }
}
