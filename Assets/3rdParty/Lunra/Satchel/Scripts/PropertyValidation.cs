using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Satchel
{
	public class PropertyValidation
	{
		public static class Default
		{
			public static class Bool
			{
				public static PropertyValidation Defined(PropertyKey<bool> key) => new PropertyValidation(
					key.Key,
					Types.Bool | Types.Defined
				);
				
				public static PropertyValidation EqualTo(PropertyKey<bool> key, bool value) => new PropertyValidation(
					key.Key,
					Types.Bool | Types.EqualTo,
					new [] { value }
				);
			}
			
			public static class Int
			{
				public static PropertyValidation Defined(PropertyKey<int> key) => new PropertyValidation(
					key.Key,
					Types.Int | Types.Defined
				);
				
				public static PropertyValidation EqualTo(PropertyKey<int> key, int value) => new PropertyValidation(
					key.Key,
					Types.Int | Types.EqualTo,
					intOperands: new [] { value }
				);
				
				public static PropertyValidation LessThan(PropertyKey<int> key, int value) => new PropertyValidation(
					key.Key,
					Types.Int | Types.LessThan,
					intOperands: new [] { value }
				);
				
				public static PropertyValidation GreaterThan(PropertyKey<int> key, int value) => new PropertyValidation(
					key.Key,
					Types.Int | Types.GreaterThan,
					intOperands: new [] { value }
				);
				
				public static PropertyValidation LessThanOrEqualTo(PropertyKey<int> key, int value) => new PropertyValidation(
					key.Key,
					Types.Int | Types.LessThan | Types.EqualTo,
					intOperands: new [] { value }
				);
				
				public static PropertyValidation GreaterThanOrEqualTo(PropertyKey<int> key, int value) => new PropertyValidation(
					key.Key,
					Types.Int | Types.GreaterThan | Types.EqualTo,
					intOperands: new [] { value }
				);
			}
			
			public static class Long
			{
				public static PropertyValidation Defined(PropertyKey<long> key) => new PropertyValidation(
					key.Key,
					Types.Long | Types.Defined
				);
				
				public static PropertyValidation EqualTo(PropertyKey<long> key, long value) => new PropertyValidation(
					key.Key,
					Types.Long | Types.EqualTo,
					longOperands: new [] { value }
				);
				
				public static PropertyValidation LessThan(PropertyKey<long> key, long value) => new PropertyValidation(
					key.Key,
					Types.Long | Types.LessThan,
					longOperands: new [] { value }
				);
				
				public static PropertyValidation GreaterThan(PropertyKey<long> key, long value) => new PropertyValidation(
					key.Key,
					Types.Long | Types.GreaterThan,
					longOperands: new [] { value }
				);
				
				public static PropertyValidation LessThanOrEqualTo(PropertyKey<long> key, long value) => new PropertyValidation(
					key.Key,
					Types.Long | Types.LessThan | Types.EqualTo,
					longOperands: new [] { value }
				);
				
				public static PropertyValidation GreaterThanOrEqualTo(PropertyKey<long> key, long value) => new PropertyValidation(
					key.Key,
					Types.Long | Types.GreaterThan | Types.EqualTo,
					longOperands: new [] { value }
				);
			}
			
			public static class Float
			{
				public static PropertyValidation Defined(PropertyKey<float> key) => new PropertyValidation(
					key.Key,
					Types.Float | Types.Defined
				);
				
				public static PropertyValidation EqualTo(PropertyKey<float> key, float value) => new PropertyValidation(
					key.Key,
					Types.Float | Types.EqualTo,
					floatOperands: new [] { value }
				);
				
				public static PropertyValidation LessThan(PropertyKey<float> key, float value) => new PropertyValidation(
					key.Key,
					Types.Float | Types.LessThan,
					floatOperands: new [] { value }
				);
				
				public static PropertyValidation GreaterThan(PropertyKey<float> key, float value) => new PropertyValidation(
					key.Key,
					Types.Float | Types.GreaterThan,
					floatOperands: new [] { value }
				);
				
				public static PropertyValidation LessThanOrEqualTo(PropertyKey<float> key, float value) => new PropertyValidation(
					key.Key,
					Types.Float | Types.LessThan | Types.EqualTo,
					floatOperands: new [] { value }
				);
				
				public static PropertyValidation GreaterThanOrEqualTo(PropertyKey<float> key, float value) => new PropertyValidation(
					key.Key,
					Types.Float | Types.GreaterThan | Types.EqualTo,
					floatOperands: new [] { value }
				);
			}
			
			public static class String
			{
				public static PropertyValidation Defined(PropertyKey<string> key) => new PropertyValidation(
					key.Key,
					Types.String | Types.Defined
				);
				
				public static PropertyValidation EqualTo(PropertyKey<string> key, string value) => new PropertyValidation(
					key.Key,
					Types.String | Types.EqualTo,
					stringOperands: new [] { value }
				);
				
				public static PropertyValidation Contains(PropertyKey<string> key, string value) => new PropertyValidation(
					key.Key,
					Types.String | Types.Contains,
					stringOperands: new [] { value }
				);
				
				public static PropertyValidation StartsWith(PropertyKey<string> key, string value) => new PropertyValidation(
					key.Key,
					Types.String | Types.StartsWith,
					stringOperands: new [] { value }
				);
				
				public static PropertyValidation EndsWith(PropertyKey<string> key, string value) => new PropertyValidation(
					key.Key,
					Types.String | Types.EndsWith,
					stringOperands: new [] { value }
				);
			}
		}
		
		[Flags]
		public enum Types
		{
			None = 0,
			Bool = 1 << 0,
			Int = 1 << 1,
			Long = 1 << 2,
			Float = 1 << 3,
			String = 1 << 4,
			
			Invert = 1 << 5,
			
			Defined = 1 << 6,
			EqualTo = 1 << 7,
			
			LessThan = 1 << 8,
			GreaterThan = 1 << 9,
			
			Contains = 1 << 10,
			StartsWith = 1 << 11,
			EndsWith = 1 << 12,
		}
		
		public enum Results
		{
			Unknown = 0,
			Ignored = 10,
			Valid = 20,
			InValid = 30
		}
		
		#region Serialized
		[JsonProperty] public string Key { get; private set; }
		[JsonProperty] public Types Type { get; private set; }

		[JsonProperty] public bool[] BoolOperands { get; private set; }
		[JsonProperty] public int[] IntOperands { get; private set; }
		[JsonProperty] public long[] LongOperands { get; private set; }
		[JsonProperty] public float[] FloatOperands { get; private set; }
		[JsonProperty] public string[] StringOperands { get; private set; }
		#endregion
		
		#region Non Serialized
		bool isInitialized;
		ValidationStore.ValidateDelegate validation;
		#endregion

		public PropertyValidation(
			string key,
			Types type,
			bool[] boolOperands = null,
			int[] intOperands = null,
			long[] longOperands = null,
			float[] floatOperands = null,
			string[] stringOperands = null
		)
		{
			Key = key;
			Type = type;
			BoolOperands = boolOperands ?? new bool[0];
			IntOperands = intOperands ?? new int[0];
			LongOperands = longOperands ?? new long[0];
			FloatOperands = floatOperands ?? new float[0];
			StringOperands = stringOperands ?? new string[0];
		}

		public PropertyValidation Initialize(ItemStore itemStore)
		{
			if (itemStore == null) throw new ArgumentNullException(nameof(itemStore));

			if (isInitialized) return this;

			isInitialized = true;
			
			var nonInvertedType = Type;
			if (nonInvertedType.HasFlag(Types.Invert)) nonInvertedType ^= Types.Invert;

			if (itemStore.Validation.All.TryGetValue(nonInvertedType, out var result))
			{
				validation = result;
			}
			else Debug.LogError($"Could not find validation for {nonInvertedType:F}");
			
			return this;
		}
		
		public Results Validate(Item item)
		{
			if (Type.HasFlag(Types.Bool))
			{
				var isDefined = item.TryGet(Key, out bool value);
				return validation(this, item, value, isDefined);
			}

			if (Type.HasFlag(Types.Int))
			{
				var isDefined = item.TryGet(Key, out int value);
				return validation(this, item, value, isDefined);
			}
			
			if (Type.HasFlag(Types.Long))
			{
				var isDefined = item.TryGet(Key, out long value);
				return validation(this, item, value, isDefined);
			}

			if (Type.HasFlag(Types.Float))
			{
				var isDefined = item.TryGet(Key, out float value);
				return validation(this, item, value, isDefined);
			}

			if (Type.HasFlag(Types.String))
			{
				var isDefined = item.TryGet(Key, out string value);
				return validation(this, item, value, isDefined);
			}
			
			Debug.LogError($"Unrecognized Type {Type:F}");
			
			return Results.Ignored;
		}
		
		bool TryGetNextOperandFromArray<A, O>(
			A[] source,
			ref int count,
			out O operand
		)
		{
			count++;
			
			if (source == null)
			{
				Debug.LogError($"{nameof(source)} was null");
				operand = default;
				return false;
			}
			if (source.Length <= count)
			{
				Debug.LogError($"Requesting index {count} of array {nameof(source)} that only has a length of {source.Length}");
				operand = default;
				return false;
			}

			if (source[count] is O typedOperand)
			{
				operand = typedOperand;
				return true;
			}

			Debug.LogError($"Incorrect array of type {typeof(A).Name} was provided for operand of type {typeof(O).Name}");
			operand = default;
			return false;
		}
		
		bool TryGetNextOperand<O>(
			ref int boolCount,
			ref int intCount,
			ref int longCount,
			ref int floatCount,
			ref int stringCount,
			out O operand
		)
		{
			if (typeof(O) == typeof(bool))
			{
				return TryGetNextOperandFromArray(
					BoolOperands,
					ref boolCount,
					out operand
				);
			}
			
			if (typeof(O) == typeof(int))
			{
				return TryGetNextOperandFromArray(
					IntOperands,
					ref intCount,
					out operand
				);
			}
			
			if (typeof(O) == typeof(long))
			{
				return TryGetNextOperandFromArray(
					LongOperands,
					ref longCount,
					out operand
				);
			}
			
			if (typeof(O) == typeof(float))
			{
				return TryGetNextOperandFromArray(
					FloatOperands,
					ref floatCount,
					out operand
				);
			}
			
			if (typeof(O) == typeof(string))
			{
				return TryGetNextOperandFromArray(
					StringOperands,
					ref stringCount,
					out operand
				);
			}

			Debug.LogError($"Unrecognized type {typeof(O).Name}");
			
			operand = default;
			return false;
		}
		
		public bool TryGetOperands<O>(
			out O operand
		)
		{
			var boolCount = -1;
			var intCount = -1;
			var longCount = -1;
			var floatCount = -1;
			var stringCount = -1;

			return TryGetNextOperand(
				ref boolCount,
				ref intCount,
				ref longCount,
				ref floatCount,
				ref stringCount,
				out operand
			);
		}
		
		public bool TryGetOperands<O0, O1>(
			out O0 operand0,
			out O1 operand1
		)
		{
			var boolCount = -1;
			var intCount = -1;
			var longCount = -1;
			var floatCount = -1;
			var stringCount = -1;

			var foundAll = TryGetNextOperand(
				ref boolCount,
				ref intCount,
				ref longCount,
				ref floatCount,
				ref stringCount,
				out operand0
			);
			
			foundAll &= TryGetNextOperand(
				ref boolCount,
				ref intCount,
				ref longCount,
				ref floatCount,
				ref stringCount,
				out operand1
			);

			return foundAll;
		}

		public override string ToString() => $"Property Validation {Type:F}";
	}
}