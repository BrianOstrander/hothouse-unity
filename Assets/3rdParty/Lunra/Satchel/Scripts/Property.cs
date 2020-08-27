using System;
using Lunra.Core;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Satchel
{
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
			if (typeof(T) == typeof(bool))
			{
				if (value is bool boolValue) property = new Property(Types.Bool, boolValue);
				else property = new Property(Types.Bool);
			}
			else if (typeof(T) == typeof(int))
			{
				if (value is int intValue) property = new Property(Types.Int, intValue: intValue);
				else property = new Property(Types.Int);
			}
			else if (typeof(T) == typeof(float))
			{
				if (value is float floatValue) property = new Property(Types.Float, floatValue: floatValue);
				else property = new Property(Types.Float);
			}
			else if (typeof(T) == typeof(string))
			{
				if (value is string stringValue) property = new Property(Types.String, stringValue: stringValue);
				else property = new Property(Types.String);
			}
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
					else Debug.LogError($"Unrecognized type {typeof(T).Name}");
					break;
				case Types.Int:
					if (IntValue is T intValue)
					{
						value = intValue;
						return true;
					}
					else Debug.LogError($"Unrecognized type {typeof(T).Name}");
					break;
				case Types.Float:
					if (FloatValue is T floatValue)
					{
						value = floatValue;
						return true;
					}
					else Debug.LogError($"Unrecognized type {typeof(T).Name}");
					break;
				case Types.String:
					if (StringValue is T stringValue)
					{
						value = stringValue;
						return true;
					}
					else Debug.LogError($"Unrecognized type {typeof(T).Name}");
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
					// Since strings are nullable, we need some special handling if we try to set it to null.
					// TODO: Handle when nonstring values get sent to this, right now it will assume you meant null...
					
					if (!(value is string stringValue)) stringValue = null;
					
					newResult = Result<Property>.Success(
						new Property(
							Type,
							stringValue: stringValue
						)
					);
					isUpdated = stringValue != StringValue;
					isReplacement = isUpdated;
					return true;
				default:
					newResult = Result<Property>.Failure($"Unrecognized property type {Type}");
					break;
			}

			isUpdated = false;
			isReplacement = false;
			return false;
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
				case Types.String:
					serializedValue = StringValue == null ? "null" : $"\"{StringValue}\""; 
					break;
				default: serializedValue = $"< Unrecognized Type {Type} >"; break;
			}

			var result = includeType ? $"{Type.ToString()[0]} : " : String.Empty;
				
			return result + $"{key,-48} | {serializedValue,-32} {StringExtensions.GetNonNullOrEmpty(suffix, String.Empty),-32}";
		}
	}
}