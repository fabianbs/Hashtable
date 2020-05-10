//  ******************************************************************************
//  * Copyright (c) 2020 Fabian Schiebel.
//  * All rights reserved. This program and the accompanying materials are made
//  * available under the terms of LICENSE.txt.
//  *
//  *****************************************************************************/
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Collections {
    public static class BitHelper {
        public static unsafe long LogicalRightShift(this long l, int sh) {
            ulong ul = *((ulong*) &l);
            ul >>= sh;
            return *((long*) &ul);
        }
        public static unsafe int LogicalRightShift(this int l, int sh) {
            uint ul = *((uint*) &l);
            ul >>= sh;
            return *((int*) &ul);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Log2(uint x) {
            return (uint) BitOperations.Log2(x);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Log2(ulong x) {
            return (uint) BitOperations.Log2(x);
        }
        #region "https://stackoverflow.com/questions/466204/rounding-up-to-next-power-of-2"
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static uint NextPow2(uint v) {
            v--;
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            v++;
            return v;
        }
        #endregion
    }
}
