//  ******************************************************************************
//  * Copyright (c) 2020 Fabian Schiebel.
//  * All rights reserved. This program and the accompanying materials are made
//  * available under the terms of LICENSE.txt.
//  *
//  *****************************************************************************/
using BenchmarkDotNet.Attributes;
using Collections;
using System;
using System.Collections.Generic;
using System.Numerics;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace XUnitTestProject1 {
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [CategoriesColumn]
    public class FlatHashTableBenchmark {

        [Benchmark(Baseline = true)]
        [BenchmarkCategory("Insert")]
        [Arguments(42, 100)]
        [Arguments(42, 1000)]
        [Arguments(42, 10000)]
        [Arguments(432, 100)]
        [Arguments(432, 1000)]
        [Arguments(432, 10000)]
        public ISet<int> GroundTruthAdd(int seed, int numElements) {
            var set = new HashSet<int>();
            var rnd = new Random(seed);
            for (int i = 0; i < numElements; ++i) {
                set.Add(rnd.Next(numElements * 2));
            }
            return set;
        }

        /* [Benchmark]
         [BenchmarkCategory("Insert")]
         [Arguments(42, 100)]
         [Arguments(42, 1000)]
         [Arguments(42, 10000)]
         [Arguments(432, 100)]
         [Arguments(432, 1000)]
         [Arguments(432, 10000)]
         public ISet<int> FHSAdd(int seed, int numElements) {
             var set = new FlatHashSet<int>();
             var rnd = new Random(seed);
             for (int i = 0; i < numElements; ++i) {
                 set.Add(rnd.Next());
             }
             return set;
         }*/

        [Benchmark]
        [BenchmarkCategory("Insert")]
        [Arguments(42, 100)]
        [Arguments(42, 1000)]
        [Arguments(42, 10000)]
        [Arguments(432, 100)]
        [Arguments(432, 1000)]
        [Arguments(432, 10000)]
        public ISet<int> VFHSAdd(int seed, int numElements) {
            var set = new VectorFlatHashSet<int>();
            var rnd = new Random(seed);
            for (int i = 0; i < numElements; ++i) {
                set.Add(rnd.Next(numElements * 2));
            }
            return set;
        }

        [Benchmark(Baseline = true)]
        [BenchmarkCategory("ReserveInsert")]
        [Arguments(42, 100)]
        [Arguments(42, 1000)]
        [Arguments(42, 10000)]
        [Arguments(432, 100)]
        [Arguments(432, 1000)]
        [Arguments(432, 10000)]
        public ISet<int> RGroundTruthAdd(int seed, int numElements) {
            var set = new HashSet<int>(numElements);
            var rnd = new Random(seed);
            for (int i = 0; i < numElements; ++i) {
                set.Add(rnd.Next(numElements * 2));
            }
            return set;
        }

        /*[Benchmark]
        [BenchmarkCategory("ReserveInsert")]
        [Arguments(42, 100)]
        [Arguments(42, 1000)]
        [Arguments(42, 10000)]
        [Arguments(432, 100)]
        [Arguments(432, 1000)]
        [Arguments(432, 10000)]
        public ISet<int> RFHSAdd(int seed, int numElements) {
            var set = new FlatHashSet<int>();
            set.Reserve((uint) numElements);
            var rnd = new Random(seed);
            for (int i = 0; i < numElements; ++i) {
                set.Add(rnd.Next());
            }
            return set;
        }*/

        [Benchmark]
        [BenchmarkCategory("ReserveInsert")]
        [Arguments(42, 100)]
        [Arguments(42, 1000)]
        [Arguments(42, 10000)]
        [Arguments(432, 100)]
        [Arguments(432, 1000)]
        [Arguments(432, 10000)]
        public ISet<int> RVFHSAdd(int seed, int numElements) {
            var set = new VectorFlatHashSet<int>((uint)numElements);
            var rnd = new Random(seed);
            for (int i = 0; i < numElements; ++i) {
                set.Add(rnd.Next(numElements * 2));
            }
            return set;
        }

        [Benchmark(Baseline = true)]
        [BenchmarkCategory("ReserveInsertString")]
        [Arguments(42, 100u)]
        [Arguments(42, 1000u)]
        [Arguments(42, 10000u)]
        [Arguments(432, 100u)]
        [Arguments(432, 1000u)]
        [Arguments(432, 10000u)]
        public ISet<string> RGroundTruthAddStr(int seed, uint numElements) {
            var set = new HashSet<string>((int)numElements);
            var rnd = new Random(seed);
            for (int i = 0; i < numElements; ++i) {
                set.Add(RandomString(BitOperations.Log2(numElements), rnd));
            }
            return set;
        }


        [Benchmark]
        [BenchmarkCategory("ReserveInsertString")]
        [Arguments(42, 100u)]
        [Arguments(42, 1000u)]
        [Arguments(42, 10000u)]
        [Arguments(432, 100u)]
        [Arguments(432, 1000u)]
        [Arguments(432, 10000u)]
        public ISet<string> RVFHSAddStr(int seed, uint numElements) {
            var set = new VectorFlatHashSet<string>(numElements);
            var rnd = new Random(seed);
            for (int i = 0; i < numElements; ++i) {
                set.Add(RandomString(BitOperations.Log2(numElements), rnd));
            }
            return set;
        }
        public static int NextExcept(HashSet<int> set, Random prng) {
            int ret;
            do {
                ret = prng.Next();
            } while (set.Contains(ret));
            return ret;
        }
        public static readonly string alphabeth =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789?!\"§$%&/()=?{[]}#+*'~,-.;:_<>|";
        private static string RandomString(int len, Random prng) {
            return string.Create(len, prng, (buf, rng) => {
                for (int i = 0; i < buf.Length; ++i)
                    buf[i] = alphabeth[rng.Next(alphabeth.Length)];
            });
        }
        public class LookupTest {
            private Random _prng;
            private HashSet<int> _inputsGt;
            private VectorFlatHashSet<int> _inputsTestSimd;
            private int[] _lookups;

            [Params(42, 422, 543)]
            public int Seed { get; set; }

            [Params(10000, 30000, 90000, 200000)]
            public int NumEntries { get; set; }
            [Params(1000, 2000)]
            public int NumLookups { get; set; }

            public VectorFlatHashSet<int> VInputsTest => _inputsTestSimd;



            [GlobalSetup]
            public void Initialize() {
                _prng = new Random(Seed);
                _lookups = new int[NumLookups];
                _inputsGt = new HashSet<int>(NumEntries);

                _inputsTestSimd = new VectorFlatHashSet<int>();
                _inputsTestSimd.Reserve((uint) NumEntries);

                for (int i = 0; i < _lookups.Length; ++i) {
                    if ((i & 1) == 0 || _inputsGt.Count == NumEntries)
                        _lookups[i] = ~_prng.Next();
                    else {
                        var num = NextExcept(_inputsGt, _prng);
                        _lookups[i] = num;
                        _inputsGt.Add(num);
                        _inputsTestSimd.Add(num);
                    }
                }

                var len = NumEntries;
                while (_inputsGt.Count < len) {
                    var num = NextExcept(_inputsGt, _prng);
                    _inputsGt.Add(num);
                    _inputsTestSimd.Add(num);
                }
            }

            [Benchmark(Baseline = true)]
            public int GroundTruth() {
                int res = 0;
                foreach (var x in _lookups) {
                    if (_inputsGt.Contains(x))
                        res += x;
                }
                return res;
            }
            /*[Benchmark]
            public int FHSLookup() {
                int res = 0;
                foreach (var x in _lookups) {
                    if (_inputsTest.Contains(x))
                        res += x;
                }
                return res;
            }*/
            [Benchmark]
            public int VFHSLookup() {
                int res = 0;
                foreach (var x in _lookups) {
                    if (_inputsTestSimd.Contains(x))
                        res += x;
                }
                return res;
            }

        }

        public class ForEachTest {
            private Random prng;

            private HashSet<int> gt;
            private VectorFlatHashSet<int> set;

            [Params(42, 345, 765)]
            public int Seed { get; set; }
            [Params(12, 34, 2345, 33456)]
            public int NumEntries { get; set; }
            [GlobalSetup]
            public void Initialize() {
                prng = new Random(Seed);

                gt = new HashSet<int>(NumEntries);
                set = new VectorFlatHashSet<int>((uint) NumEntries);

                for (uint i = 0; i < NumEntries; ++i) {
                    var x = NextExcept(gt, prng);
                    gt.Add(x);
                    set.Add(x);
                }
            }

            [Benchmark(Baseline = true)]
            public int GTForEach() {
                int ret = 42;
                foreach (var x in gt) {
                    ret ^= x;
                }

                return ret;
            }

            [Benchmark]
            public int VFHSForEachIt() {
                int ret = 42;
                foreach (var x in set) {
                    ret ^= x;
                }

                return ret;
            }
            [Benchmark]
            public int VFHSForEachJIt() {
                int ret = 42;
                var it = new VectorFlatHashTable<int>.JIterator(set);
                while (it.HasNext()) {
                    ret ^= it.Next();
                }

                return ret;
            }
            [Benchmark]
            public int VFHSForEach() {
                int ret = 42;
                set.ForEach(x => ret ^= x);

                return ret;
            }
        }
        public class RemoveTest {

            private HashSet<int> gt;
            private VectorFlatHashSet<int> set;

            [Params(42, 345, 765)]
            public int Seed { get; set; }
            [Params(12, 34, 2345, 33456)]
            public int NumEntries { get; set; }
            [GlobalSetup]
            public void Initialize() {
                var prng = new Random(Seed);

                gt = new HashSet<int>(NumEntries);
                set = new VectorFlatHashSet<int>((uint) NumEntries);

                for (uint i = 0; i < NumEntries; ++i) {
                    var x = NextExcept(gt, prng);
                    gt.Add(x);
                    set.Add(x);
                }
            }

            [Benchmark(Baseline = true)]
            public int GTRemove() {
                var prng = new Random(Seed);
                int ret = 0;
                for (int i = 0; i < NumEntries; ++i) {
                    if (gt.Remove(prng.Next()))
                        ret++;
                }

                return ret;
            }


            [Benchmark]
            public int VFHRemove() {
                var prng = new Random(Seed);
                int ret = 0;
                for (int i = 0; i < NumEntries; ++i) {
                    if (set.Remove(prng.Next()))
                        ret++;
                }

                return ret;
            }
        }
        public static void Main() {
            BenchmarkRunner.Run<FlatHashTableBenchmark>();
            BenchmarkRunner.Run<LookupTest>();
            BenchmarkRunner.Run<ForEachTest>();
            BenchmarkRunner.Run<RemoveTest>();
            // BenchmarkRunner.Run<SwapBenchmark>();


            /*var lt = new LookupTest {
                NumEntries = 30000,
                NumLookups = 100000
            };
            lt.Initialize();
            Console.WriteLine(lt.VFHSLookup());
            Console.WriteLine($"Load: {lt.VInputsTest.CurrentLoad}");*/
            /*for (int i = 0; i < 100; ++i) {
                var FHT = new FlatHashTableBenchmark();
                _ = FHT.VFHSAdd(42, 100000);
                _ = FHT.GroundTruthAdd(42, 100000);
            }*/

        }
    }
}
