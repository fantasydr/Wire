// -----------------------------------------------------------------------
//   <copyright file="Serializer.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

namespace Wire
{
    public class Helper
    {
        public class Dictionary<TKey, TValue> : System.Collections.Generic.Dictionary<TKey, TValue>
        {
            public Dictionary(System.Collections.Generic.IEqualityComparer<TKey> comparer) 
                : base(comparer)
            {

            }

            public Dictionary() : base ()
            {

            }

            public bool TryAdd(TKey key, TValue val)
            {
                if (this.ContainsKey(key))
                    return false;

                this.Add(key, val);
                return true;
            }

            public TValue GetOrAdd(TKey key, System.Func<TKey, TValue> valueFactory)
            {
                if (this.ContainsKey(key))
                    return this[key];

                TValue val = valueFactory(key);
                this.Add(key, val);
                return val;
            }
        }
    }
}