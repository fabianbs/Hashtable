//  ******************************************************************************
//  * Copyright (c) 2020 Fabian Schiebel.
//  * All rights reserved. This program and the accompanying materials are made
//  * available under the terms of LICENSE.txt.
//  *
//  *****************************************************************************/
using System;
using System.Collections.Generic;

namespace Collections {
    public static class Dictionary {
        private static class DictionaryHelper<TKey, TValue> where TKey : notnull {
            public static readonly Lazy<IReadOnlyDictionary<TKey, TValue>> Instance 
                = new Lazy<IReadOnlyDictionary<TKey, TValue>>(() => new Dictionary<TKey, TValue>());
        }
        public static IReadOnlyDictionary<TKey, TValue> Empty<TKey, TValue>() where TKey : notnull {
            return DictionaryHelper<TKey, TValue>.Instance.Value;
        }

    }
}
