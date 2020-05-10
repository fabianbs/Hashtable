//  ******************************************************************************
//  * Copyright (c) 2020 Fabian Schiebel.
//  * All rights reserved. This program and the accompanying materials are made
//  * available under the terms of LICENSE.txt.
//  *
//  *****************************************************************************/

using System;
using BenchmarkDotNet.Attributes;

namespace XUnitTestProject1 {
    public class SwapBenchmark {

        static void TupleSwapper<T>(ref T x, ref T y) {
            (y, x) = (x, y);
        }

        static void RingSwapper<T>(ref T x, ref T y) {
            T tmp = x;
            x = y;
            y = tmp;
        }

        [Benchmark]
        [Arguments(42, 1000)]
        [Arguments(42, 10000)]
        [Arguments(432, 1000)]
        [Arguments(432, 10000)]
        public int RingSwap(int seed, int numSamples) {
            var rnd = new Random(seed);
            var ret = 0;

            for (int i = 0; i < numSamples; ++i) {
                int num = rnd.Next();
                RingSwapper(ref num, ref ret);
                ret ^= num >> 3;
            }

            return ret;
        }
        [Arguments(42, 1000)]
        [Arguments(42, 10000)]
        [Arguments(432, 1000)]
        [Arguments(432, 10000)]
        [Benchmark]
        public int TupleSwap(int seed, int numSamples) {
            var rnd = new Random(seed);
            var ret = 0;

            for (int i = 0; i < numSamples; ++i) {
                int num = rnd.Next();
                TupleSwapper(ref num, ref ret);
                ret ^= num >> 3;
            }

            return ret;
        }
    }
}