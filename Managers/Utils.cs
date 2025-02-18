using System;
using System.Collections.Generic;

namespace Anthology.Models
{
	public class MKDictionary<TKey1,TKey2,TValue> :  Dictionary<Tuple<TKey1, TKey2>, TValue>, IDictionary<Tuple<TKey1, TKey2>, TValue> {
		public TValue this[TKey1 key1, TKey2 key2] {
			get { return base[Tuple.Create(key1, key2)]; }
			
			set { base[Tuple.Create(key1, key2)] = value; }
		}

		public void Add(TKey1 key1, TKey2 key2, TValue value)
		{
			base.Add(Tuple.Create(key1, key2), value);
		}

		public bool ContainsKey(TKey1 key1, TKey2 key2)
		{
			return base.ContainsKey(Tuple.Create(key1, key2));
		}
	}
}