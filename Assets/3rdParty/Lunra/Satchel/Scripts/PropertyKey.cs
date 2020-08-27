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
		public PropertyKeyValue Pair(T value = default)
		{
			Property.TryNew(value, out var property);
			
			return new PropertyKeyValue(
				Key,
				property
			);
		}
	}
}