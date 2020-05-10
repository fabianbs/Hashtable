//  ******************************************************************************
//  * Copyright (c) 2020 Fabian Schiebel.
//  * All rights reserved. This program and the accompanying materials are made
//  * available under the terms of LICENSE.txt.
//  *
//  *****************************************************************************/
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Collections {
    /// <summary>
    /// A (unordered) hash set built on top of <see cref="VectorFlatHashTable{T}"/>
    /// </summary>
    /// <typeparam name="T">The type of the elements in the set</typeparam>
    public class VectorFlatHashSet<T> : VectorFlatHashTable<T>, ISet<T> {
        /// <inheritdoc />
        public VectorFlatHashSet(IEqualityComparer<T>? cmp = null) : base(cmp) { }
        /// <inheritdoc />
        public VectorFlatHashSet(uint initialCapacity, IEqualityComparer<T>? cmp = null) : base(initialCapacity, cmp) { }
        /// <summary>
        /// Constructor. Inserts the elements from <paramref name="content"/> into the newly allocated set
        /// </summary>
        /// <param name="content">The elements to insert initially</param>
        public VectorFlatHashSet(IEnumerable<T> content) {
            UnionWith(content);
        }
        /// <summary>
        /// The number of elements stored in this set
        /// </summary>
        public uint Size => size;
        /// <summary>
        /// The number of elements stored in this set
        /// </summary>
        public int Count => (int) size;
        /// <summary>
        /// This set is not readonly
        /// </summary>
        public bool IsReadOnly => false;
        /// <inheritdoc />
        public bool Add(T item) {
            return Insert(item, false);
        }
        /// <inheritdoc />
        void ICollection<T>.Add(T item) {
            Add(item);
        }
        /// <inheritdoc />
        public void Clear() {
            ClearInternal();
        }
        /// <inheritdoc />
        public bool Contains(T item) {
            return ContainsInternal(item);
        }
        /// <inheritdoc />
        public void CopyTo(T[] array, int arrayIndex) {
           
            ForEach(x => {
                if (arrayIndex >= array.Length)
                    return false;

                array[arrayIndex++] = x;
                return true;
            });
        }
        /// <inheritdoc />
        public void ExceptWith(IEnumerable<T> other) {
            foreach (var item in other)
                Remove(item);
        }
        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator() {
            var it = new JIterator(this);
            while (it.HasNext())
                yield return it.Next();
        }
        /// <inheritdoc />
        public void IntersectWith(IEnumerable<T> other) {
            ISet<T> oSet = other is ISet<T> set ? set : new VectorFlatHashSet<T>(other);

            //Vector<T> toRemove = default;
            var toRemove = new List<T>();

            ForEach(item => {
                if (!oSet.Contains(item))
                    toRemove.Add(item);
            });
            foreach (var x in toRemove)
                Remove(x);
        }
        /// <inheritdoc />
        public bool IsProperSubsetOf(IEnumerable<T> other) {
            return IsSubsetOf(other, out uint ctr) && ctr != Size;
        }
        /// <inheritdoc />
        public bool IsProperSupersetOf(IEnumerable<T> other) {
            return IsSuperSetOf(other, out uint ctr) && ctr != Size;
        }
        /// <inheritdoc />
        public bool IsSubsetOf(IEnumerable<T> other) {
            return IsSubsetOf(other, out _);
        }

        public bool IsSupersetOf(VectorFlatHashSet<T> other) {
            if (other.Size > Size) {
                return false;
            }

            return other.ForEach(Contains) == Size;
        }
        /// <inheritdoc />
        public bool IsSupersetOf(IEnumerable<T> other) {
            return IsSuperSetOf(other, out _);
        }
        /// <inheritdoc />
        public bool Overlaps(IEnumerable<T> other) {
            return other.Any(Contains);
        }
        /// <inheritdoc />
        public bool Remove(T item) {
            return RemoveInternal(item);
        }
        /// <inheritdoc />
        /// <remarks>Validate correctness</remarks>
        public bool SetEquals(IEnumerable<T> other) {
            if (other is ICollection coll) {
                return Count == coll.Count && IsSupersetOf(other);
            }

            var tmp = new VectorFlatHashSet<T>(other);
            if (tmp.Size != Size)
                return false;

            return tmp.Size > Size ? tmp.IsSupersetOf(this) : IsSupersetOf(tmp);
        }
        /// <inheritdoc />
        public void SymmetricExceptWith(IEnumerable<T> other) {
            foreach (var item in other.Where(item => !Remove(item)))
                Add(item);
        }
        /// <inheritdoc />
        public void UnionWith(IEnumerable<T> other) {
            if (other is ICollection coll) {
                Reserve(coll.UCount());
            }

            foreach (var item in other)
                Add(item);
        }

        private bool IsSubsetOf(IEnumerable<T> other, out uint otherCount) {
            if (other is ICollection coll && coll.Count < Count) {
                otherCount = coll.UCount();
                return false;
            }

            var oSet = other is ISet<T> set ? set : new VectorFlatHashSet<T>(other);

            otherCount = oSet.UCount();

            /*var it = new JIterator(this);
            while (it.HasNext()) {
                if (!oSet.Contains(it.Next()))
                    return false;
            }
            return true;*/

            return ForEach(x => oSet.Contains(x)) == Size;
        }

        private bool IsSuperSetOf(IEnumerable<T> other, out uint otherCount) {
            if (other is ICollection coll) {
                if (coll.Count > Count) {
                    otherCount = (uint) coll.Count;
                    return false;
                }
            }

            otherCount = 0;
            foreach (var item in other) {
                if (!Contains(item))
                    return false;

                otherCount++;
            }

            return true;
        }
    }
}