using System;
using System.Collections.Generic;
using Lunra.Core;
using Newtonsoft.Json;

namespace Lunra.Satchel
{
	public class Item
	{
		[JsonProperty] public ulong Id { get; private set; }
		[JsonProperty] public DateTime LastUpdated { get; private set; }
		[JsonProperty] public bool IsInitialized { get; private set; }
		
		[JsonProperty] Dictionary<string, string> stringProperties = new Dictionary<string, string>();
		[JsonProperty] Dictionary<string, int> intProperties = new Dictionary<string, int>();
		[JsonProperty] Dictionary<string, float> floatProperties = new Dictionary<string, float>();
		[JsonProperty] Dictionary<string, bool> boolProperties = new Dictionary<string, bool>();

		ItemStore itemStore;
		Action<DateTime, ItemStore.UpdateTypes, Item> itemStoreUpdated;
			
		public event Action<DateTime, ItemStore.UpdateTypes, Item> Updated;
		
		public Item(ulong id)
		{
			Id = id;
		}

		public void Initialize(
			ItemStore itemStore,
			Action<DateTime, ItemStore.UpdateTypes, Item> updated
		)
		{
			this.itemStore = itemStore;
			itemStoreUpdated = updated;

			if (!IsInitialized) TriggerNewUpdate();
		}
		
		public bool TryGet<T>(ItemKey<T> key, out T value)
		{
			value = default;
			var found = false;
			if (key.Type == typeof(string))
			{
				found = stringProperties.TryGetValue(key.Key, out var rawValue);
				if (found && rawValue is T castValue) value = castValue;
			}
			else if (key.Type == typeof(int))
			{
				found = intProperties.TryGetValue(key.Key, out var rawValue);
				if (found && rawValue is T castValue) value = castValue;
			}
			else if (key.Type == typeof(float))
			{
				found = floatProperties.TryGetValue(key.Key, out var rawValue);
				if (found && rawValue is T castValue) value = castValue;
			}
			else if (key.Type == typeof(bool))
			{
				found = boolProperties.TryGetValue(key.Key, out var rawValue);
				if (found && rawValue is T castValue) value = castValue;
			}
			else throw new ArgumentOutOfRangeException(typeof(T).FullName);

			return found;
		}

		public T Get<T>(ItemKey<T> key, T defaultValue = default)
		{
			if (TryGet(key, out var value)) return value;
			return defaultValue;
		}
		
		public T Get<T>(string key, T defaultValue = default)
		{
			if (TryGet(new ItemKey<T>(key), out var value)) return value;
			return defaultValue;
		}
		
		public Item Set<T>(ItemKey<T> key, T value)
		{
			if (key.Type == typeof(string) && value is string stringValue) stringProperties[key.Key] = stringValue;
			else if (key.Type == typeof(int) && value is int intValue) intProperties[key.Key] = intValue;
			else if (key.Type == typeof(float) && value is float floatValue) floatProperties[key.Key] = floatValue;
			else if (key.Type == typeof(bool) && value is bool boolValue) boolProperties[key.Key] = boolValue;
			else throw new ArgumentOutOfRangeException(typeof(T).FullName);
			
			TriggerUpdate();
			return this;
		}
		
		public Item Set(string key, string value)
		{
			stringProperties[key] = value;
			TriggerUpdate();
			return this;
		}

		public Item Set(string key, int value)
		{
			intProperties[key] = value;
			TriggerUpdate();
			return this;
		}
		
		public Item Set(string key, float value)
		{
			floatProperties[key] = value;
			TriggerUpdate();
			return this;
		}
		
		public Item Set(string key, bool value)
		{
			boolProperties[key] = value;
			TriggerUpdate();
			return this;
		}

		void TriggerNewUpdate()
		{
			LastUpdated = DateTime.Now;
			IsInitialized = true;

			itemStoreUpdated(LastUpdated, ItemStore.UpdateTypes.ItemNew, this);
			Updated?.Invoke(LastUpdated, ItemStore.UpdateTypes.ItemNew, this);
		}

		void TriggerUpdate()
		{
			LastUpdated = DateTime.Now;

			if (IsInitialized)
			{
				itemStoreUpdated(LastUpdated, ItemStore.UpdateTypes.ItemUpdated, this);
				Updated?.Invoke(LastUpdated, ItemStore.UpdateTypes.ItemNew, this);
			}
		}

		public override string ToString()
		{
			var result = $"Item Id: {Id}";

			foreach (var kv in stringProperties)
			{
				result += $"\n - [ string ] \t {kv.Key} : \t{kv.Value}";
			}
			foreach (var kv in intProperties)
			{
				result += $"\n - [ int ] \t {kv.Key} : \t{kv.Value}";
			}
			foreach (var kv in floatProperties)
			{
				result += $"\n - [ float ] \t {kv.Key} : \t{kv.Value}";
			}
			foreach (var kv in boolProperties)
			{
				result += $"\n - [ bool ] \t {kv.Key} : \t{kv.Value}";
			}

			return result;
		}
	}
}