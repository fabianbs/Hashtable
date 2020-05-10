//  ******************************************************************************
//  * Copyright (c) 2020 Fabian Schiebel.
//  * All rights reserved. This program and the accompanying materials are made
//  * available under the terms of LICENSE.txt.
//  *
//  *****************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using SimdVec = System.Numerics.Vector;

namespace Collections {
    public static class EnumerableHelper {
       
        internal class EnumerableCollection<T> : ICollection<T>, IReadOnlyCollection<T> {
            private readonly IEnumerable<T> underlying;

            public EnumerableCollection(IEnumerable<T> it, int count) {
                underlying = it;
                Count = count;
            }

            public int Count {
                get;
            }
            public bool IsReadOnly {
                get => true;
            }

            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(T item) => underlying.Contains(item);
            public void CopyTo(T[] array, int arrayIndex) {
                if (arrayIndex < 0)
                    throw new IndexOutOfRangeException(nameof(arrayIndex));
                using var it = underlying.GetEnumerator();
                for (int i = 0; it.MoveNext() && i < Count && arrayIndex < array.Length; ++i) {
                    array[arrayIndex++] = it.Current;
                }
            }
            public IEnumerator<T> GetEnumerator() {
                using var it = underlying.GetEnumerator();
                for (int i = 0; it.MoveNext() && i < Count; ++i) {
                    yield return it.Current;
                }
            }
            public bool Remove(T item) => throw new NotSupportedException();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        internal class AsReadOnlyCollection<T> : IReadOnlyCollection<T>, ICollection<T> {
            private readonly ICollection<T> coll;
            public AsReadOnlyCollection(ICollection<T> col) {
                coll = col ;
            }
            public int Count => coll.Count;

            public bool IsReadOnly => true;

            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();

            public bool Contains(T item) => coll.Contains(item);
            public void CopyTo(T[] array, int arrayIndex) => coll.CopyTo(array, arrayIndex);
            public IEnumerator<T> GetEnumerator() => coll.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
        public static ICollection<T> AsCollection<T>(this IEnumerable<T> it, int count = int.MaxValue) {
            if (it is ICollection<T> coll)
                return coll;

            return new EnumerableCollection<T>(it, count);
        }
        public static IReadOnlyCollection<T> Append<T>(this ICollection<T> coll, T val) {
            return new EnumerableCollection<T>((coll as IEnumerable<T>).Concat(new[] { val }), coll.Count + 1);
        }

        public static void AddRange<T>(this ICollection<T> coll, IEnumerable<T> values) {
            if (coll is List<T> list) {
                list.AddRange(values);
            }
            else {
                foreach (var x in values) {
                    coll.Add(x);
                }
            }
        }
        public static void ForEach<T>(this IEnumerable<T> iter, Action<T> fe) {
            foreach (var x in iter) {
                fe(x);
            }
        }
       
        public static IReadOnlyCollection<T> AsReadOnly<T>(this ICollection<T> coll) {
            if (coll is IReadOnlyCollection<T> roc)
                return roc;

            return new AsReadOnlyCollection<T>(coll);
        }
    }
    public static class Collection {
        public static ICollection<T> Repeat<T>(T val, int count) {
            return new EnumerableHelper.EnumerableCollection<T>(Enumerable.Repeat(val, count), count);
        }

        public static uint UCount(this ICollection coll) => unchecked((uint) coll.Count);
        public static uint UCount<T>(this ICollection<T> coll) => unchecked((uint) coll.Count);
        public static uint UCount<T>(this IReadOnlyCollection<T> coll) => unchecked((uint) coll.Count);
    }
    
    public static class ArrayHelper {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ULength<T>(this T[] arr) {
            return unchecked((uint) arr.LongLength);
        }
    }
}
