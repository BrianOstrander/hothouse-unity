using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Lunra.Core;
using UnityEngine;

namespace Lunra.Satchel
{
	public class ValidationStore
	{
		public delegate PropertyValidation.Results ValidateDelegate(PropertyValidation validation, Item item, object value, bool isDefined);
		
		ItemStore itemStore;

		Dictionary<PropertyValidation.Types, ValidateDelegate> all;
		public ReadOnlyDictionary<PropertyValidation.Types, ValidateDelegate> All { get; private set; } 
		
		public ValidationStore Initialize(
			ItemStore itemStore
		)
		{
			this.itemStore = itemStore;

			all = new Dictionary<PropertyValidation.Types, ValidateDelegate>();
			All = new ReadOnlyDictionary<PropertyValidation.Types, ValidateDelegate>(all);

			InitializeValidators<bool>();
			InitializeValidators<int>();
			InitializeValidators<float>();
			InitializeValidators<string>();

			return this;
		}

		void InitializeValidators<T>()
		{
			foreach (var propertyValidationOperationType in ReflectionUtility.GetTypesWithAttribute<PropertyValidationOperationAttribute, PropertyValidationOperation<T>>(true))
			{
				if (ReflectionUtility.TryGetInstanceOfType<PropertyValidationOperation<T>>(propertyValidationOperationType, out var propertyValidationOperationInstance))
				{
					try
					{
						propertyValidationOperationInstance.Initialize(itemStore);
						if (all.TryGetValue(propertyValidationOperationInstance.Type, out var existingValue))
						{
							Debug.LogError($"Tried to bind {propertyValidationOperationInstance.GetType().Name} of Type {propertyValidationOperationInstance.Type} but {existingValue.GetType().Name} is already bound to it");
						}
						else
						{
							all[propertyValidationOperationInstance.Type] = (validation, item, value, isDefined) =>
							{
								if (value is T typedValue) return propertyValidationOperationInstance.Validate(validation, item, typedValue, isDefined);
								Debug.LogError($"Provided a value of type {(value == null ? "null" : value.GetType().Name)} when expecting {typeof(T).Name}");
								return PropertyValidation.Results.Ignored;
							};
						}
					}
					catch (Exception e)
					{
						Debug.LogException(e);
					}
				}
			}
		}
	}
}