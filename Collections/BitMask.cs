//  ******************************************************************************
//  * Copyright (c) 2020 Fabian Schiebel.
//  * All rights reserved. This program and the accompanying materials are made
//  * available under the terms of LICENSE.txt.
//  *
//  *****************************************************************************/
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Collections {
    /// <summary>
    /// A helper class representing a set of unsigned integers
    /// </summary>
    public struct BitMask : IEquatable<BitMask> {
        // 2^32 / phi
        private const int MAGIC_NUMBER = -1640531527;
        // The bitmask, where the indices of the 1 values represent the integers stored in the set
        private int _mask;

        public BitMask(int mask) {
            _mask = mask;
        }

        // see "https://github.com/abseil/abseil-cpp/blob/master/absl/container/internal/raw_hash_set.h"
        /// <summary>
        /// Gets and removes the next stored integer
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint Next() {
            var ret = unchecked((uint) BitOperations.TrailingZeroCount(_mask));
            _mask &= _mask - 1;
            return ret;
        }
        /// <summary>
        /// True, iff this "set" is non-empty
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has() {
            return _mask != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator BitMask(int mask) {
            return new BitMask(mask);
        }

        public override bool Equals(object? obj) {
            return obj is BitMask bm && Equals(bm);
        }
        public bool Equals(BitMask obj) {
            return _mask == obj._mask;
        }

        public override int GetHashCode() {
            return unchecked(_mask * MAGIC_NUMBER);
        }

        public static bool operator ==(BitMask left, BitMask right) {
            return left.Equals(right);
        }

        public static bool operator !=(BitMask left, BitMask right) {
            return !(left == right);
        }
    }

    /// <summary>
    /// A helper class which extends an <see langword="int"/>, such that it supports the same interface as <see cref="BitMask"/>
    /// </summary>
    public static class BitMaskHelper {
        /// <summary>
        /// True, if there is a 1 bit in <paramref name="_mask"/>
        /// </summary>
        /// <param name="_mask"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Has(this int _mask) {
            return _mask != 0;
        }

        /// <summary>
        /// Gets the index of the least significant 1 bit in <paramref name="_mask"/> and sets this bit to 0
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static uint Next(this ref int _mask) {
            var ret = unchecked((uint) BitOperations.TrailingZeroCount(_mask));
            _mask &= _mask - 1;
            return ret;
        }
        /// <summary>
        /// Gets the index of the least significant 1 bit in <paramref name="_mask"/> and computes a successor mask, where this bit is set to 0
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static (uint, int) NextT(this int _mask) {
            var ret = unchecked((uint) BitOperations.TrailingZeroCount(_mask));
            return (ret, _mask & (_mask - 1));
        }
        /// <summary>
        /// Gets the index of the least significant 1 bit in <paramref name="_mask"/>
        /// </summary>
        /// <param name="_mask"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static uint Current(this int _mask) {
            return unchecked((uint) BitOperations.TrailingZeroCount(_mask));
        }
        /// <summary>
        /// Sets the least significant 1 bit in <paramref name="_mask"/> to 0
        /// </summary>
        /// <param name="_mask"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static int MoveNext(this int _mask) {
            return _mask & (_mask - 1);
        }
    }
}
