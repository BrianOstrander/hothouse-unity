using System;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Lunra.Satchel
{
	public interface IPropertyKey
	{
		string Key { get; }
		Type Type { get; }
	}
	
	public struct PropertyKey<T> : IPropertyKey
	{
		public string Key { get; }
		public Type Type { get; }
		
		public PropertyKey(string key)
		{
			Key = key;
			Type = typeof(T);
		}
		
		[Pure]
		public bool IsPropertyEqual(Item item0, Item item1) => Item.IsPropertyEqual(Key, item0, item1);
		
		[Pure]
		public PropertyKeyValue Pair(T value = default)
		{
			Property.TryNew(value, out var property);
			
			return new PropertyKeyValue(
				Key,
				property
			);
		}
		
		public PropertyKeyValue Pair(
			PropertyKeyValue[] elements
		)
		{
			var key = Key;
			return new PropertyKeyValue(
				Key,
				elements.First(e => e.Key == key).Property
			);
		}
	}
}