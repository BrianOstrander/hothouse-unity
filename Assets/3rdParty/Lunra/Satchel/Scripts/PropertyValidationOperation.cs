using Lunra.Core;
using UnityEngine;

namespace Lunra.Satchel
{
	public interface IPropertyValidationOperation
	{
		PropertyValidation.Types Type { get; }
	}
	
	[PropertyValidationOperation]
	public abstract class PropertyValidationOperation<T> : IPropertyValidationOperation
	{
		protected readonly struct RequestPayload
		{
			public ItemStore ItemStore { get; }
			public PropertyValidation Validation { get; }
			public Item Item { get; }
			public T Value { get; }

			public RequestPayload(
				ItemStore itemStore,
				PropertyValidation validation,
				Item item,
				T value
			)
			{
				ItemStore = itemStore;
				Validation = validation;
				Item = item;
				Value = value;
			}
		}

		public PropertyValidation.Types Type => ValueType | OperationType;

		public PropertyValidation.Types ValueType
		{
			get
			{
				if (typeof(T) == typeof(bool)) return PropertyValidation.Types.Bool;
				if (typeof(T) == typeof(int)) return PropertyValidation.Types.Int;
				if (typeof(T) == typeof(float)) return PropertyValidation.Types.Float;
				if (typeof(T) == typeof(string)) return PropertyValidation.Types.String;
				Debug.LogError($"Unrecognized type: {typeof(T).Name}");
				return PropertyValidation.Types.None;
			}
		}
		public abstract PropertyValidation.Types OperationType { get; }

		ItemStore itemStore;

		public PropertyValidationOperation<T> Initialize(ItemStore itemStore)
		{
			this.itemStore = itemStore;

			var foundOperationType = false;
			
			foreach (var type in EnumExtensions.GetValues(PropertyValidation.Types.None))
			{
				switch (type)
				{
					case PropertyValidation.Types.Bool:
					case PropertyValidation.Types.Int:
					case PropertyValidation.Types.Float:
					case PropertyValidation.Types.String:
						if (OperationType.HasFlag(type)) Debug.LogError($"OperationType has invalid type: {type}");
						break;
					
					case PropertyValidation.Types.Invert:
						if (OperationType.HasFlag(type)) Debug.LogError($"Operations should not define type {type}");
						break;
					case PropertyValidation.Types.EqualTo:
					case PropertyValidation.Types.LessThan:
					case PropertyValidation.Types.GreaterThan:
					case PropertyValidation.Types.Contains:
					case PropertyValidation.Types.StartsWith:
					case PropertyValidation.Types.EndsWith:
						if (ValueType.HasFlag(type)) Debug.LogError($"ValueType has invalid type: {type}");
						if (OperationType.HasFlag(type)) foundOperationType = true;
						break;
					default:
						Debug.LogError($"Unrecognized Type: {type}");
						break;
				}
			}
			
			if (!foundOperationType) Debug.LogError("Could not find operation type!");

			return this;
		}
		
		public PropertyValidation.Results Validate(
			PropertyValidation validation,
			Item item,
			T value
		)
		{
			var request = new RequestPayload(
				itemStore,
				validation,
				item,
				value
			);

			if (IsIgnored(request)) return PropertyValidation.Results.Ignored;
			return (IsValid(request) && !validation.Type.HasFlag(PropertyValidation.Types.Invert)) ? PropertyValidation.Results.Valid : PropertyValidation.Results.InValid;
		}

		protected virtual bool IsIgnored(RequestPayload request) => false;
		protected abstract bool IsValid(RequestPayload request);

		public override string ToString() => $"{ValueType}.{OperationType:F} | {typeof(T).Name} | {GetType().Name}";
	}
}