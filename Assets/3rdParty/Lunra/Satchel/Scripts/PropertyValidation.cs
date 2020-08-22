using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Satchel
{
	public class PropertyValidation
	{
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
		[JsonProperty] public Types Type { get; private set; }
		[JsonProperty] public string Key { get; private set; }

		[JsonProperty] public bool[] BoolOperands { get; private set; }
		[JsonProperty] public int[] IntOperands { get; private set; }
		[JsonProperty] public float[] FloatOperands { get; private set; }
		[JsonProperty] public string[] StringOperands { get; private set; }
		#endregion
		
		#region Non Serialized
		ItemStore itemStore;
		ValidationStore.ValidateDelegate validation;
		#endregion

		public PropertyValidation(
			Types type,
			bool[] boolOperands,
			int[] intOperands,
			float[] floatOperands,
			string[] stringOperands
		)
		{
			Type = type;
			BoolOperands = boolOperands;
			IntOperands = intOperands;
			FloatOperands = floatOperands;
			StringOperands = stringOperands;
		}

		public PropertyValidation Initialize(ItemStore itemStore)
		{
			this.itemStore = itemStore;

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
			if (Type.HasFlag(Types.Bool)) return validation(this, item, item.Get<bool>(Key));
			if (Type.HasFlag(Types.Int)) return validation(this, item, item.Get<int>(Key));
			if (Type.HasFlag(Types.Float)) return validation(this, item, item.Get<float>(Key));
			if (Type.HasFlag(Types.String)) return validation(this, item, item.Get<string>(Key));
			
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