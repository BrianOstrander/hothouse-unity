using System;

namespace Lunra.Satchel
{
	public struct PropertyKey<T>
	{
		public string Key { get; }
		public Type Type { get; }
		
		public PropertyKey(string key)
		{
			Key = key;
			Type = typeof(T);
		}
	}
}