using System;

namespace Lunra.Satchel
{
	public struct ItemKey<T>
	{
		public string Key { get; }
		public Type Type { get; }
		
		public ItemKey(string key)
		{
			Key = key;
			Type = typeof(T);
		}
	}
}