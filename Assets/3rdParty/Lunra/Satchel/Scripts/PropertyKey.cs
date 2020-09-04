using System;
using System.Diagnostics.Contracts;
using Lunra.Core;

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

		public override string ToString() => $"{StringExtensions.GetNonNullOrEmpty(Key, "[ null or empty ]")}<{Type.ToString().ToLower()[0]}>";
	}
}