//  ******************************************************************************
//  * Copyright (c) 2020 Fabian Schiebel.
//  * All rights reserved. This program and the accompanying materials are made
//  * available under the terms of LICENSE.txt.
//  *
//  *****************************************************************************/
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Collections {
    public static class Map {
        private class EmptyDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue> where TKey : notnull {
            public TValue this[TKey key] => throw new KeyNotFoundException();

            public IEnumerable<TKey> Keys {
                get {
                    yield break;
                }
            }
            public IEnumerable<TValue> Values {
                get {
                    yield break;
                }
            }
            public int Count => 0;

            public bool ContainsKey(TKey key) => false;
            public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
                yield break;
            }

            public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) {
                value = default!;
                return false;
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


            private EmptyDictionary() {

            }

            public static readonly EmptyDictionary<TKey, TValue> Instance = new EmptyDictionary<TKey, TValue>();
        }

        public static IReadOnlyDictionary<TKey, TValue> Empty<TKey, TValue>() where TKey : notnull {
            return EmptyDictionary<TKey, TValue>.Instance;
        }
    }
}