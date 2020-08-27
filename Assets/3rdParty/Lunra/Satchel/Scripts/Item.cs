using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Lunra.Core;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Satchel
{
	public class Item
	{
		public const ulong UndefinedId = 0uL;
		
		[Flags]
		public enum Formats
		{
			Default = 0,
			IncludeProperties = 1 << 0,
			ExtraPropertyIndent = 1 << 1
		}

		public struct Event
		{
			[Flags]
			public enum Types
			{
				None = 0,
				Item = 1 << 0,
				Property = 1 << 1,
				New = 1 << 2,
				Updated = 1 << 3,
				Destroyed = 1 << 4
			}
			
			[JsonProperty] public ulong Id { get; private set; }
			[JsonProperty] public DateTime UpdateTime { get; private set; }
			[JsonProperty] public Types[] Updates { get; private set; }
			[JsonProperty] public ReadOnlyDictionary<string, (Property Property, Types Update)> PropertyEvents { get; private set; }

			public Event(
				ulong id,
				DateTime updateTime,
				Types[] updates,
				ReadOnlyDictionary<string, (Property Property, Types Update)> propertyEvents
			)
			{
				Id = id;
				UpdateTime = updateTime;
				Updates = updates;
				PropertyEvents = propertyEvents;
			}

			public override string ToString() => ToString(false);
			
			public string ToString(bool verbose)
			{
				var result = $"Item [ {Id} ] |";

				foreach (var type in Updates) result += " " + ReadableUpdateType(type);

				if (PropertyEvents.Any())
				{
					result += $" | {PropertyEvents.Count} Property Update(s)";

					if (!verbose) return result;
				}
				else return result;

				foreach (var kv in PropertyEvents)
				{
					result += $"\n\t - {kv.Value.Property.ToString(kv.Key, suffix: " | " + ReadableUpdateType(kv.Value.Update))}";
				}

				return result;
			}

			public static string ReadableUpdateType(Types type)
			{
				var result = string.Empty;

				foreach (var referenceType in EnumExtensions.GetValues(Types.None))
				{
					if (type.HasFlag(referenceType)) result += referenceType;
				}

				return result;
			}
		}
		
		#region Serialized
		[JsonProperty] public ulong Id { get; private set; }
		[JsonProperty] public DateTime LastUpdated { get; private set; }
		
		[JsonProperty] bool isInitialized;
		[JsonProperty] Dictionary<string, Property> properties = new Dictionary<string, Property>();
		#endregion

		#region Non Serialized
		[JsonIgnore] public string[] PropertyKeys => properties.Keys.ToArray();

		ItemStore itemStore;
		Action<Event> itemStoreUpdated;
		#endregion

		public Item(ulong id) => Id = id;

		public void Initialize(
			ItemStore itemStore,
			Action<Event> itemStoreUpdated
		)
		{
			this.itemStore = itemStore;
			this.itemStoreUpdated = itemStoreUpdated;

			if (!isInitialized)
			{
				isInitialized = true;
				
				var eventPropertyUpdates = properties
					.ToReadonlyDictionary(
						kv => kv.Key,
						kv => (kv.Value, Event.Types.Property | Event.Types.New)
					);

				LastUpdated = DateTime.Now;
				itemStoreUpdated(
					new Event(
						Id,
						LastUpdated,
						eventPropertyUpdates.Any() ? new[] {Event.Types.Item | Event.Types.New, Event.Types.Property | Event.Types.New} : new[] {Event.Types.Item | Event.Types.New},
						eventPropertyUpdates
					)
				);
			}
		}

		public bool IsDefined<T>(string key) => TryGet<T>(key, out _);
		public bool IsDefined<T>(PropertyKey<T> key) => TryGet(key, out _);
		
		public bool TryGet<T>(string key, out T value)
		{
			value = default;

			if (properties.TryGetValue(key, out var property))
			{
				if (property.TryGet(out value)) return true;
				Debug.LogError($"Item Id [ {Id} ] found property [ {key} ] but it was of type {property.Type} not {typeof(T)}");
			}

			return false;
		}

		public bool TryGet<T>(PropertyKey<T> key, out T value) => TryGet(key.Key, out value);

		public T Get<T>(string key, T defaultValue = default) => TryGet(key, out T value) ? value : defaultValue;

		public T Get<T>(PropertyKey<T> key, T defaultValue = default) => Get(key.Key, defaultValue);

		public Item Set<T>(
			string key,
			T value,
			out (Property Property, Event.Types Update) result,
			bool suppressUpdates
		)
		{
			result = (default, Event.Types.None);
			
			if (properties.TryGetValue(key, out var existingProperty))
			{
				existingProperty.TryNewValue(
					value,
					out var newResult,
					out _,
					out var isPropertyReplacement
				);
				
				if (newResult.Status == ResultStatus.Success)
				{
					if (isPropertyReplacement)
					{
						properties[key] = newResult.Payload;
						result = (newResult.Payload, Event.Types.Property | Event.Types.Updated);
					}
				}
				else newResult.Log();
			}
			else if (Property.TryNew(value, out var newProperty))
			{
				properties[key] = newProperty;
				result = (newProperty, Event.Types.Property | Event.Types.New);
			}

			if (!suppressUpdates) TryUpdate(key, result.Property, result.Update);

			return this;
		}
		
		public Item Set<T>(string key, T value) => Set(key, value, out _, false);

		public Item Set<T>(PropertyKey<T> key, T value) => Set(key.Key, value);

		public Item Set(params PropertyKeyValue[] propertyKeyValues)
		{
			if (propertyKeyValues.None()) return this;
			
			var propertyUpdates = new List<(string Key, Property Property, Event.Types Update)>();

			foreach (var kv in propertyKeyValues)
			{
				kv.Apply(
					this,
					out var result,
					true
				);
				
				if (result.Update != Event.Types.None) propertyUpdates.Add((kv.Key, result.Property, result.Update));
			}
			
			TryUpdate(propertyUpdates.ToArray());

			return this;
		}
		
		bool TryUpdate(
			string key,
			Property property,
			Event.Types update
		)
		{
			if (!isInitialized) return false;
			if (update == Event.Types.None) return false;

			LastUpdated = DateTime.Now;
			itemStoreUpdated(
				new Event(
					Id,
					LastUpdated,
					update.WrapInArray(),
					new ReadOnlyDictionary<string, (Property Property, Event.Types Update)>(
						new Dictionary<string, (Property Property, Event.Types Update)>
						{
							{ key, (property, update) }
						}
					)
				)
			);
			
			return true;
		}
		
		bool TryUpdate(
			params (string Key, Property Property, Event.Types Update)[] propertyUpdates 
		)
		{
			if (!isInitialized) return false;
			if (propertyUpdates.None()) return false;
			
			var eventUpdates = Event.Types.None;
			var eventPropertyUpdates = new Dictionary<string, (Property Property, Event.Types Update)>();

			foreach (var propertyUpdate in propertyUpdates)
			{
				if (propertyUpdate.Update == Event.Types.None) continue;
				eventUpdates |= propertyUpdate.Update;

				eventPropertyUpdates[propertyUpdate.Key] = (propertyUpdate.Property, propertyUpdate.Update);
			}

			if (eventUpdates == Event.Types.None) return false;
			
			LastUpdated = DateTime.Now;
			itemStoreUpdated(
				new Event(
					Id,
					LastUpdated,
					eventUpdates.WrapInArray(),
					eventPropertyUpdates.ToReadonlyDictionary()
				)
			);

			return true;
		}

		public void CloneProperties(
			Item source,
			params string[] ignoredKeys
		)
		{
			var propertyKeyValues = new List<PropertyKeyValue>();
			
			foreach (var property in source.properties)
			{
				if (ignoredKeys.Contains(property.Key)) continue;
				propertyKeyValues.Add(new PropertyKeyValue(property.Key, property.Value));
			}

			if (propertyKeyValues.None()) return;
			
			Set(propertyKeyValues.ToArray());
		}

		public Stack StackOfZero() => StackOf(0);
		public Stack StackOf(int count) => new Stack(Id, count);

		/// <summary>
		/// Used, ideally only, by the ItemStore to update this value upon destruction.
		/// </summary>
		/// <param name="lastUpdated"></param>
		public void ForceUpdateTime(DateTime lastUpdated) => LastUpdated = lastUpdated;

		public bool Is(ulong id) => id == Id;

		public bool Is(Item item) => Is(item.Id);
		
		public static bool IsPropertyEqual(Item item0, Item item1, string key)
		{
			if (item0 == null) throw new ArgumentNullException(nameof(item0));
			if (item1 == null) throw new ArgumentNullException(nameof(item1));
			if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
			
			if (item0.Id == item1.Id) return true;
			
			var found0 = item0.properties.TryGetValue(key, out var property0);
			var found1 = item1.properties.TryGetValue(key, out var property1);

			if (found0 != found1) return false;
			if (found0 == false) return true;

			return property0.IsEqualTo(property1);
		}

		public bool CanStack(Item other)
		{
			if (!isInitialized) throw new NonInitializedInventoryOperationException(nameof(CanStack));

			return itemStore.CanStack(this, other);
		}
		
		public override string ToString() => ToString(Formats.Default);

		public string ToString(int count) => ToString(Formats.Default, count);
		
		public string ToString(Formats format, int? count = null)
		{
			var result = $"[ {Id} ]";

			if (count.HasValue) result += $" | Count: {count.Value}";

			result += $" | {(isInitialized ? "Initialized" : "Not Initialized")} | {LastUpdated}";

			if (format.HasFlag(Formats.IncludeProperties))
			{
				foreach (var kv in properties.OrderBy(kv => kv.Key))
				{
					result += $"\n\t";

					if (format.HasFlag(Formats.ExtraPropertyIndent)) result += "\t";

					result += $" - {kv.Value.ToString(kv.Key)}";
				}
			}
			else result += $" | {properties.Count} Property(s)";

			return result;
		}
	}
}