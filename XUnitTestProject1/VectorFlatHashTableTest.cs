//  ******************************************************************************
//  * Copyright (c) 2020 Fabian Schiebel.
//  * All rights reserved. This program and the accompanying materials are made
//  * available under the terms of LICENSE.txt.
//  *
//  *****************************************************************************/
using System;
using System.Collections.Generic;
using Collections;
using Xunit;
using Xunit.Abstractions;

namespace XUnitTestProject1 {
    public class VectorFlatHashTableTest {
        public VectorFlatHashTableTest(ITestOutputHelper _cout) {
            cout = _cout;
        }

        public static readonly string alphabeth =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789?!\"§$%&/()=?{[]}#+*'~,-.;:_<>|";

        private readonly ITestOutputHelper cout;

        public static int NextExcept(Random prng, HashSet<int> set) {
            int ret;
            do {
                ret = prng.Next();
            } while (set.Contains(ret));

            return ret;
        }

        private void ComputeIfAbsentTest(int seed) {
            var prng = new Random(seed);

            const uint NUM_ENTRIES = 2398;
            var gt = new Dictionary<int, int>((int) NUM_ENTRIES);
            var map = new VectorFlatHashMap<int, int>(NUM_ENTRIES);

            Assert.Equal(0u, map.Size);

            for (uint i = 0; i < NUM_ENTRIES; ++i) {
                int x = prng.Next((int) NUM_ENTRIES * 2);

                int y = map.ComputeIfAbsent(x, a => {
                    Assert.Equal(x, a);
                    Assert.False(gt.ContainsKey(a));
                    return a * a;
                });
                Assert.Equal(x * x, y);
                gt[x] = x * x;
            }

            Assert.Equal(gt.Count, map.Count);
            foreach ((int key, int value) in map) {
                Assert.Equal(key * key, value);
                Assert.Contains(new KeyValuePair<int, int>(key, value), gt);
            }
        }

        private void ForEachTest(int seed) {
            var prng = new Random(seed);

            const uint NUM_ENTRIES = 2343;
            const uint len = 10;

            var gt = new HashSet<string>((int) NUM_ENTRIES);
            var set = new VectorFlatHashSet<string>(NUM_ENTRIES);

            for (uint i = 0; i < NUM_ENTRIES; ++i) {
                string str = RandomString((int) len, prng);

                set.Add(str);
                gt.Add(str);

                Assert.Contains(str, set);

                if ((i & 1) != 0) {
                    string rem = RandomString((int) len, prng);

                    bool remSet = set.Remove(rem);
                    bool remGt = gt.Remove(rem);

                    Assert.Equal(remGt, remSet);
                    Assert.DoesNotContain(rem, set);

                    if (remGt) {
                        str = RandomString((int) len, prng);

                        set.Add(str);
                        gt.Add(str);

                        Assert.Contains(str, set);
                    }
                }
            }

            Assert.Equal(gt.Count, set.Count);

            var fe = new HashSet<string>((int) NUM_ENTRIES);
            foreach (string x in set) {
                Assert.True(fe.Add(x));
                Assert.Contains(x, gt);
            }

            Assert.Equal(gt.Count, fe.Count);

            fe.Clear();

            set.ForEach(x => {
                Assert.True(fe.Add(x));
                Assert.Contains(x, gt);
            });

            Assert.Equal(gt.Count, fe.Count);
        }

        private void InsertLookup(int seed) {
            var set = new VectorFlatHashSet<int>();
            //set.Reserve(10000);
            var gt = new HashSet<int>(10000);
            var notContained = //Vector<int>.Reserve(10000);
                new List<int>(10000);

            var prng = new Random(seed);

            for (int i = 0; i < 10000; ++i) {
                int num = NextExcept(prng, gt);
                if ((i & 1) == 0) {
                    gt.Add(num);
                    set.Add(num);
                }
                else
                    notContained.Add(num);
            }

            foreach (int x in gt)
                Assert.Contains(x, set);

            foreach (int x in notContained)
                Assert.DoesNotContain(x, set);
        }

        private void MergeTest(int seed) {
            var prng = new Random(seed);

            const uint NUM_ENTRIES = 3214;

            var gt = new Dictionary<int, int>((int) NUM_ENTRIES);
            var map = new VectorFlatHashMap<int, int>(NUM_ENTRIES);

            static int Merger(int x, int y) {
                return x * y + 1;
            }

            void GtMerge(int key, int value) {
                if (gt.TryGetValue(key, out int oldValue))
                    gt[key] = Merger(oldValue, value);
                else
                    gt[key] = value;
            }

            for (uint i = 0; i < NUM_ENTRIES; ++i) {
                int x = prng.Next((int) NUM_ENTRIES * 2);

                GtMerge(x, x + 1);
                map.Merge(x, x + 1, Merger);
            }

            Assert.Equal(gt.Count, map.Count);

            foreach ((int key, int value) in map)
                Assert.Contains(new KeyValuePair<int, int>(key, value), gt);
        }

        private string RandomString(int len, Random prng) {
            return string.Create(len, prng, (buf, rng) => {
                for (int i = 0; i < buf.Length; ++i)
                    buf[i] = alphabeth[rng.Next(alphabeth.Length)];
            });
        }

        private void RemoveTest(int seed) {
            var set1 = new VectorFlatHashSet<string>();
            var set2 = new HashSet<string>();

            // explicit seed to make the test reproducable
            var prng = new Random(seed);

            const int strlen = 10;
            const int len = 1000;
            for (int i = 0; i < len; ++i) {
                string str = RandomString(strlen, prng);
                bool succ1 = set1.Add(str);
                bool succ2 = set2.Add(str);
                Assert.Equal(succ2, succ1);
                Assert.Equal(set2.Count, set1.Count);
                Assert.Contains(str, set1);
                if ((i & 1) == 1) {
                    string rem = RandomString(strlen, prng);
                    succ1 = set1.Remove(rem);
                    succ2 = set2.Remove(rem);
                    Assert.Equal(succ2, succ1);
                    Assert.Equal(set2.Count, set1.Count);
                    Assert.DoesNotContain(rem, set1);
                }
            }

            Assert.Equal(set2.Count, set1.Count);
            foreach (string x in set1)
                Assert.Contains(x, set2);
            foreach (string x in set2)
                Assert.Contains(x, set1);
        }

        [Fact]
        public void Basic1() {
            var set1 = new VectorFlatHashSet<int>();
            int[] testInput =
                {1, 3, 5, 7, 9, 8, 6, 3, 4, 2, 3, 5, 6, 7, 8, 9, 2, 3, 4, 1, 2, 3, 5, 6, 4, 3, 5, 8, 7, 9, 0, 8, 6};
            int[] testOutput = {0, 1, 2, 3, 4, 5, 6, 7, 8, 9};
            foreach (int x in testInput)
                set1.Add(x);

            Assert.NotEmpty(set1);
            Assert.Equal(10u, set1.Size);

            foreach (int x in testOutput)
                Assert.Contains(x, set1);

            uint count = 0;
            foreach (int x in set1)
                count++;
            Assert.Equal(testOutput.ULength(), count);
        }

        [Fact]
        public void Basic2() {
            var map1 = new VectorFlatHashMap<int, int>();
            (int, int)[] testInput = {(1, 1), (2, 3), (3, 5), (5, 8), (8, 13), (13, 21), (21, 34), (21, 33)};
            foreach ((int x, int y) in testInput)
                map1.Add(x, y);

            Assert.NotEmpty(map1);

            Assert.Equal(7u, map1.Size);

            foreach ((int x, int y) in testInput.AsSpan(0, testInput.Length - 1)) {
                Assert.True(map1.ContainsKey(x));
                Assert.Equal(y, map1[x]);
            }

            Assert.DoesNotContain(new KeyValuePair<int, int>(21, 33), map1);
        }

        [Fact]
        public void ComputeIfAbsentTest1() {
            ComputeIfAbsentTest(42);
        }

        [Fact]
        public void ComputeIfAbsentTest2() {
            ComputeIfAbsentTest(234);
        }

        [Fact]
        public void ForEachTest1() {
            ForEachTest(42);
        }

        [Fact]
        public void ForEachTest2() {
            ForEachTest(442);
        }

        [Fact]
        public void InsertLookup1() {
            InsertLookup(42);
        }

        [Fact]
        public void InsertLookup2() {
            InsertLookup(422);
        }

        [Fact]
        public void MergeTest1() {
            MergeTest(42);
        }

        [Fact]
        public void MergeTest2() {
            MergeTest(654);
        }

        [Fact]
        public void RemoveTest1() {
            RemoveTest(42);
        }

        [Fact]
        public void RemoveTest2() {
            RemoveTest(422);
        }
    }
}