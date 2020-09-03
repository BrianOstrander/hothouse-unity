using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Lunra.Satchel
{
	public struct PropertyKeyValue
	{
		public string Key { get; }
		public Property Property { get; }

		public PropertyKeyValue(
			string key,
			Property property
		)
		{
			Key = key;
			Property = property;
		}
		
		public bool IsEqualToValueIn(
			Item item
		)
		{
			switch (Property.Type)
			{
				case Property.Types.Bool:
					return item.TryGet<bool>(Key, out var boolValue) && boolValue == Property.BoolValue;
				case Property.Types.Int:
					return item.TryGet<int>(Key, out var intValue) && intValue == Property.IntValue;
				case Property.Types.Long:
					return item.TryGet<long>(Key, out var longValue) && longValue == Property.LongValue;
				case Property.Types.Float:
					return item.TryGet<float>(Key, out var floatValue) && Mathf.Approximately(floatValue, Property.FloatValue);
				case Property.Types.String:
					return item.TryGet<string>(Key, out var stringValue) && stringValue == Property.StringValue;
				default: 
					Debug.LogError("Unrecognized Type: "+Property.Type);
					return false;
			}
		}
		
		public void Apply(
			Item item
		)
		{
			switch (Property.Type)
			{
				case Property.Types.Bool:
					item.Set(Key, Property.BoolValue);
					break;
				case Property.Types.Int:
					item.Set(Key, Property.IntValue);
					break;
				case Property.Types.Long:
					item.Set(Key, Property.LongValue);
					break;
				case Property.Types.Float:
					item.Set(Key, Property.FloatValue);
					break;
				case Property.Types.String:
					item.Set(Key, Property.StringValue);
					break;
				default: 
					Debug.LogError("Unrecognized Type: "+Property.Type);
					break;
			}
		}

		public void Apply(
			Item item,
			out (Property Property, Item.Event.Types Update) result,
			bool suppressUpdates
		)
		{
			switch (Property.Type)
			{
				case Property.Types.Bool:
					item.Set(
						Key,
						Property.BoolValue,
						out result,
						suppressUpdates
					);
					break;
				case Property.Types.Int:
					item.Set(
						Key,
						Property.IntValue,
						out result,
						suppressUpdates
					);
					break;
				case Property.Types.Long:
					item.Set(
						Key,
						Property.LongValue,
						out result,
						suppressUpdates
					);
					break;
				case Property.Types.Float:
					item.Set(
						Key,
						Property.FloatValue,
						out result,
						suppressUpdates
					);
					break;
				case Property.Types.String:
					item.Set(
						Key,
						Property.StringValue,
						out result,
						suppressUpdates
					);
					break;
				default: 
					Debug.LogError("Unrecognized Type: "+Property.Type);
					result = default;
					break;
			}
		}

		public static implicit operator PropertyKeyValue((PropertyKey<bool> Key, bool Value) pair)
		{
			return new PropertyKeyValue(
				pair.Key.Key,
				new Property(Property.Types.Bool, pair.Value)
			);
		}
		
		public static implicit operator PropertyKeyValue((PropertyKey<int> Key, int Value) pair)
		{
			return new PropertyKeyValue(
				pair.Key.Key,
				new Property(Property.Types.Int, intValue: pair.Value)
			);
		}
		
		public static implicit operator PropertyKeyValue((PropertyKey<long> Key, long Value) pair)
		{
			return new PropertyKeyValue(
				pair.Key.Key,
				new Property(Property.Types.Long, longValue: pair.Value)
			);
		}
		
		public static implicit operator PropertyKeyValue((PropertyKey<float> Key, float Value) pair)
		{
			return new PropertyKeyValue(
				pair.Key.Key,
				new Property(Property.Types.Float, floatValue: pair.Value)
			);
		}
		
		public static implicit operator PropertyKeyValue((PropertyKey<string> Key, string Value) pair)
		{
			return new PropertyKeyValue(
				pair.Key.Key,
				new Property(Property.Types.String, stringValue: pair.Value)
			);
		}
	}

	public static class PropertyKeyValueExtensions
	{
		public static T FirstValueForKey<T>(
			this PropertyKeyValue[] elements,
			PropertyKey<T> key
		)
		{
			if (!elements.First(e => e.Key == key.Key).Property.TryGet<T>(out var result))
			{
				throw new Exception($"Unable to find matching key for {key.Key} among specified elements");
			}

			return result;
		}
	}
}