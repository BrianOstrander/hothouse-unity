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

		[JsonProperty] public bool[] BoolOperands { get; private set; }
		[JsonProperty] public int[] IntOperands { get; private set; }
		[JsonProperty] public float[] FloatOperands { get; private set; }
		[JsonProperty] public string[] StringOperands { get; private set; }
		#endregion
		
		#region Non Serialized
		ItemStore itemStore;
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
			return this;
		}
		
		public Results Validate(
			Item item,
			string key,
			object value
		)
		{
			switch (value)
			{
				case bool boolValue: 
					return OnValidate(item, key, boolValue);
				case int intValue: 
					return OnValidate(item, key, intValue);
				case float floatValue: 
					return OnValidate(item, key, floatValue);
				case string stringValue: 
					return OnValidate(item, key, stringValue);
				default:
					// Debug.LogError($"Unrecognized value type: {typeof(T).Name}");
					return Results.Ignored;
			}
		}

		protected virtual Results OnValidate(Item item, string key, bool value) => Results.Ignored;
		protected virtual Results OnValidate(Item item, string key, int value) => Results.Ignored;
		protected virtual Results OnValidate(Item item, string key, float value) => Results.Ignored;
		protected virtual Results OnValidate(Item item, string key, string value) => Results.Ignored;
		
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
	}
}