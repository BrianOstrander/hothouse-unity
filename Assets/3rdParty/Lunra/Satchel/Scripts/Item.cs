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
		[Flags]
		public enum Formats
		{
			Default = 0,
			IncludeProperties = 1 << 0,
			ExcludePrefix = 1 << 1,
			ExtraPropertyIndent = 1 << 2
		}
		
		public struct Property
		{
			public enum Types
			{
				Unknown = 0,
				Bool = 10,
				Int = 20,
				Float = 30,
				String = 40
			}
			
			public static bool TryNew<T>(
				T value,
				out Property property
			)
			{
				if (value is bool boolValue) property = new Property(Types.Bool, boolValue);
				else if (value is int intValue) property = new Property(Types.Int, intValue: intValue);
				else if (value is float floatValue) property = new Property(Types.Float, floatValue: floatValue);
				else if (value is string stringValue) property = new Property(Types.String, stringValue: stringValue);
				else
				{
					Debug.LogError("Unrecognized value type "+typeof(T));
					property = default;
					return false;
				}

				return true;
			}
			
			[JsonProperty] public Types Type { get; private set; }
			
			[JsonProperty] public bool BoolValue { get; private set; }
			[JsonProperty] public int IntValue { get; private set; }
			[JsonProperty] public float FloatValue { get; private set; }
			[JsonProperty] public string StringValue { get; private set; }

			public Property(
				Types type,
				bool boolValue = false,
				int intValue = 0,
				float floatValue = 0f,
				string stringValue = null
			)
			{
				Type = type;
				BoolValue = boolValue;
				IntValue = intValue;
				FloatValue = floatValue;
				StringValue = stringValue;
			}
			
			public bool Is<T>()
			{
				switch (Type)
				{
					case Types.Bool:
						return typeof(T) == typeof(bool);
					case Types.Int:
						return typeof(T) == typeof(int);
					case Types.Float:
						return typeof(T) == typeof(float);
					case Types.String:
						return typeof(T) == typeof(string);
					default:
						Debug.LogError("Unrecognized property type: "+Type);
						return false;
				}
			}
			
			public bool TryGet<T>(out T value)
			{
				value = default;
				
				switch (Type)
				{
					case Types.Bool:
						if (BoolValue is T boolValue)
						{
							value = boolValue;
							return true;
						}
						break;
					case Types.Int:
						if (IntValue is T intValue)
						{
							value = intValue;
							return true;
						}
						break;
					case Types.Float:
						if (FloatValue is T floatValue)
						{
							value = floatValue;
							return true;
						}
						break;
					case Types.String:
						if (StringValue is T stringValue)
						{
							value = stringValue;
							return true;
						}
						break;
					default:
						Debug.LogError("Unrecognized property type: "+Type);
						break;
				}
				
				return false;
			}
			
			public bool TryNewValue<T>(
				T value,
				out Result<Property> newResult,
				out bool isUpdated,
				out bool isReplacement
			)
			{
				switch (Type)
				{
					case Types.Bool:
						if (value is bool boolValue)
						{
							newResult = Result<Property>.Success(
								new Property(
									Type,
									boolValue
								)
							);
							isUpdated = boolValue != BoolValue;
							isReplacement = isUpdated;
							return true;
						}
						
						newResult = Result<Property>.Failure($"Expected new value of type {Type}, but it was a {value.GetType().Name}");
						break;
					case Types.Int:
						if (value is int intValue)
						{
							newResult = Result<Property>.Success(
								new Property(
									Type,
									intValue: intValue
								)
							);
							isUpdated = intValue != IntValue;
							isReplacement = isUpdated;
							return true;
						}
						
						newResult = Result<Property>.Failure($"Expected new value of type {Type}, but it was a {value.GetType().Name}");
						break;
					case Types.Float:
						if (value is float floatValue)
						{
							newResult = Result<Property>.Success(
								new Property(
									Type,
									floatValue: floatValue
								)
							);
							isUpdated = !Mathf.Approximately(floatValue, FloatValue);
							isReplacement = true;
							return true;
						}
						
						newResult = Result<Property>.Failure($"Expected new value of type {Type}, but it was a {value.GetType().Name}");
						break;
					case Types.String:
						if (value is string stringValue)
						{
							newResult = Result<Property>.Success(
								new Property(
									Type,
									stringValue: stringValue
								)
							);
							isUpdated = stringValue != StringValue;
							isReplacement = isUpdated;
							return true;
						}
						
						newResult = Result<Property>.Failure($"Expected new value of type {Type}, but it was a {value.GetType().Name}");
						break;
					default:
						newResult = Result<Property>.Failure($"Unrecognized property type {Type}");
						break;
				}

				isUpdated = false;
				isReplacement = false;
				return false;
			}

			public bool TryGetRawValue(out object value)
			{
				switch (Type)
				{
					case Types.Bool:
						value = BoolValue;
						return true;
					case Types.Int:
						value = IntValue;
						return true;
					case Types.Float:
						value = FloatValue;
						return true;
					case Types.String:
						value = StringValue;
						return true;
					default:
						value = null;
						Debug.LogError($"Unrecognized Type {Type}");
						return false;
				}
			}

			public bool IsEqualTo(Property property)
			{
				if (Type != property.Type) return false;
				switch (Type)
				{
					case Types.Bool:
						return BoolValue == property.BoolValue;
					case Types.Int:
						return IntValue == property.IntValue;
					case Types.Float:
						return Mathf.Approximately(FloatValue, property.FloatValue);
					case Types.String:
						return StringValue == property.StringValue;
					default:
						Debug.LogError("Unrecognized Type: "+Type);
						return true;
				}
			}

			public override string ToString() => ToString("< Unknown Key >");

			public string ToString(
				string key,
				bool includeType = false,
				string suffix = null
			)
			{
				string serializedValue;

				switch (Type)
				{
					case Types.Bool: serializedValue = BoolValue.ToString().ToLower(); break;
					case Types.Int: serializedValue = IntValue.ToString(); break;
					case Types.Float: serializedValue = FloatValue.ToString("N4"); break;
					case Types.String: serializedValue = $"\"{StringValue}\""; break;
					default: serializedValue = $"< Unrecognized Type {Type} >"; break;
				}

				var result = includeType ? $"{Type.ToString()[0]} : " : string.Empty;
				
				return result + $"{key,-32} | {serializedValue,-32} {StringExtensions.GetNonNullOrEmpty(suffix, string.Empty),-32}";
			}
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
		
		[JsonProperty] public ulong Id { get; private set; }
		[JsonProperty] public DateTime LastUpdated { get; private set; }
		
		[JsonProperty] bool isInitialized;
		[JsonProperty] Dictionary<string, Property> properties = new Dictionary<string, Property>();

		[JsonIgnore] public string[] PropertyKeys => properties.Keys.ToArray();
		
		Action<Event> itemStoreUpdated;

		public Item(ulong id) => Id = id;

		public void Initialize(Action<Event> itemStoreUpdated)
		{
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

		public bool TryGet<T>(ItemKey<T> key, out T value) => TryGet(key.Key, out value);

		public T Get<T>(string key, T defaultValue = default) => TryGet(key, out T value) ? value : defaultValue;

		public T Get<T>(ItemKey<T> key, T defaultValue = default) => Get(key.Key, defaultValue);

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

		public Item Set<T>(ItemKey<T> key, T value) => Set(key.Key, value);

		public Item Set(params (string Key, object Value)[] propertyKeyValues)
		{
			if (propertyKeyValues.None()) return this;
			
			var propertyUpdates = new List<(string Key, Property Property, Event.Types Update)>();

			foreach (var kv in propertyKeyValues)
			{
				Set(
					kv.Key,
					kv.Value,
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
			var propertyKeyValues = new List<(string Key, object Value)>();
			
			foreach (var property in source.properties)
			{
				if (ignoredKeys.Contains(property.Key)) continue;
				if (property.Value.TryGetRawValue(out var value)) propertyKeyValues.Add((property.Key, value));
			}

			if (propertyKeyValues.None()) return;
			
			Set(propertyKeyValues.ToArray());
		}
		
		/// <summary>
		/// Used, ideally only, by the ItemStore to update this value upon destruction.
		/// </summary>
		/// <param name="lastUpdated"></param>
		public void ForceUpdateTime(DateTime lastUpdated) => LastUpdated = lastUpdated;

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
		
		public override string ToString() => ToString(Formats.Default);
		
		public string ToString(Formats format)
		{
			var result = format.HasFlag(Formats.ExcludePrefix) ? $"[ {Id} ]" : $"Item Id: {Id}";

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