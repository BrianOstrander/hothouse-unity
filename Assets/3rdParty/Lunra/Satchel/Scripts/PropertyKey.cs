using System;
using System.Diagnostics.Contracts;

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
	}
}