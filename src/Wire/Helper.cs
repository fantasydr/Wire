// -----------------------------------------------------------------------
//   <copyright file="Serializer.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

namespace Wire
{
    public class Helper
    {
        public class HashSet<T> : System.Collections.Generic.ICollection<T>
        {
            private Dictionary<T, bool> _innerDictionary;

            public HashSet()
            {
                _innerDictionary = new Dictionary<T, bool>();
            }

            void System.Collections.Generic.ICollection<T>.Add(T item)
            {
                AddInternal(item);
            }

            private void AddInternal(T item)
            {
                _innerDictionary.Add(item, false);
            }

            public bool Add(T item)
            {
                if (_innerDictionary.ContainsKey(item))
                    return false;

                AddInternal(item);
                return true;
            }

            public void Clear()
            {
                _innerDictionary.Clear();
                _innerDictionary = new Dictionary<T, bool>();
            }

            public bool Contains(T item)
            {
                return _innerDictionary.ContainsKey(item);
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
                _innerDictionary.Keys.CopyTo(array, arrayIndex);
            }

            public int Count
            {
                get { return _innerDictionary.Keys.Count; }
            }

            public bool IsReadOnly
            {
                get
                {
                    return false;
                }
            }

            public bool Remove(T item)
            {
                return _innerDictionary.Remove(item);
            }

            public System.Collections.Generic.IEnumerator<T> GetEnumerator()
            {
                return _innerDictionary.Keys.GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

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