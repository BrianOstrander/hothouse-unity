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
				public static PropertyValidation EqualTo(string key, bool value) => new PropertyValidation(
					key,
					Types.Bool | Types.EqualTo,
					new [] { value }
				);
			}
			
			public static class Int
			{
				public static PropertyValidation EqualTo(string key, int value) => new PropertyValidation(
					key,
					Types.Int | Types.EqualTo,
					intOperands: new [] { value }
				);
				
				public static PropertyValidation LessThan(string key, int value) => new PropertyValidation(
					key,
					Types.Int | Types.LessThan,
					intOperands: new [] { value }
				);
				
				public static PropertyValidation GreaterThan(string key, int value) => new PropertyValidation(
					key,
					Types.Int | Types.GreaterThan,
					intOperands: new [] { value }
				);
				
				public static PropertyValidation LessThanOrEqualTo(string key, int value) => new PropertyValidation(
					key,
					Types.Int | Types.LessThan | Types.EqualTo,
					intOperands: new [] { value }
				);
				
				public static PropertyValidation GreaterThanOrEqualTo(string key, int value) => new PropertyValidation(
					key,
					Types.Int | Types.GreaterThan | Types.EqualTo,
					intOperands: new [] { value }
				);
			}
			
			public static class Float
			{
				public static PropertyValidation EqualTo(string key, float value) => new PropertyValidation(
					key,
					Types.Float | Types.EqualTo,
					floatOperands: new [] { value }
				);
				
				public static PropertyValidation LessThan(string key, float value) => new PropertyValidation(
					key,
					Types.Float | Types.LessThan,
					floatOperands: new [] { value }
				);
				
				public static PropertyValidation GreaterThan(string key, float value) => new PropertyValidation(
					key,
					Types.Float | Types.GreaterThan,
					floatOperands: new [] { value }
				);
				
				public static PropertyValidation LessThanOrEqualTo(string key, float value) => new PropertyValidation(
					key,
					Types.Float | Types.LessThan | Types.EqualTo,
					floatOperands: new [] { value }
				);
				
				public static PropertyValidation GreaterThanOrEqualTo(string key, float value) => new PropertyValidation(
					key,
					Types.Float | Types.GreaterThan | Types.EqualTo,
					floatOperands: new [] { value }
				);
			}
			
			public static class String
			{
				public static PropertyValidation EqualTo(string key, string value) => new PropertyValidation(
					key,
					Types.String | Types.EqualTo,
					stringOperands: new [] { value }
				);
				
				public static PropertyValidation Contains(string key, string value) => new PropertyValidation(
					key,
					Types.String | Types.Contains,
					stringOperands: new [] { value }
				);
				
				public static PropertyValidation StartsWith(string key, string value) => new PropertyValidation(
					key,
					Types.String | Types.StartsWith,
					stringOperands: new [] { value }
				);
				
				public static PropertyValidation EndsWith(string key, string value) => new PropertyValidation(
					key,
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
			Float = 1 << 2,
			String = 1 << 3,
			
			Invert = 1 << 4,
			
			EqualTo = 1 << 5,
			LessThan = 1 << 6,
			GreaterThan = 1 << 7,
			
			Contains = 1 << 8,
			StartsWith = 1 << 9,
			EndsWith = 1 << 10,
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
		[JsonProperty] public float[] FloatOperands { get; private set; }
		[JsonProperty] public string[] StringOperands { get; private set; }
		#endregion
		
		#region Non Serialized
		ValidationStore.ValidateDelegate validation;
		#endregion

		public PropertyValidation(
			string key,
			Types type,
			bool[] boolOperands = null,
			int[] intOperands = null,
			float[] floatOperands = null,
			string[] stringOperands = null
		)
		{
			Key = key;
			Type = type;
			BoolOperands = boolOperands ?? new bool[0];
			IntOperands = intOperands ?? new int[0];
			FloatOperands = floatOperands ?? new float[0];
			StringOperands = stringOperands ?? new string[0];
		}

		public PropertyValidation Initialize(ItemStore itemStore)
		{
			if (itemStore == null) throw new ArgumentNullException(nameof(itemStore));

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
				if (item.TryGet(Key, out bool value)) return validation(this, item, value);
				return Results.InValid;
			}

			if (Type.HasFlag(Types.Int))
			{
				if (item.TryGet(Key, out int value)) return validation(this, item, value);
				return Results.InValid;
			}

			if (Type.HasFlag(Types.Float))
			{
				if (item.TryGet(Key, out float value)) return validation(this, item, value);
				return Results.InValid;
			}

			if (Type.HasFlag(Types.String))
			{
				if (item.TryGet(Key, out string value)) return validation(this, item, value);
				return Results.InValid;
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
			var floatCount = -1;
			var stringCount = -1;

			return TryGetNextOperand(
				ref boolCount,
				ref intCount,
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
			var floatCount = -1;
			var stringCount = -1;

			var foundAll = TryGetNextOperand(
				ref boolCount,
				ref intCount,
				ref floatCount,
				ref stringCount,
				out operand0
			);
			
			foundAll &= TryGetNextOperand(
				ref boolCount,
				ref intCount,
				ref floatCount,
				ref stringCount,
				out operand1
			);

			return foundAll;
		}

		public override string ToString() => $"Property Validation {Type:F}";
	}
}