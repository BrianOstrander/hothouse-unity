using System;
using System.Collections.Generic;

namespace Lunra.Satchel
{
	public class InventoryFilterBuilder
	{
		ItemStore itemStore;
		int limit = int.MaxValue;
		PropertyFilterBuilder propertyFilterBuilder;

		/// <summary>
		/// An item filter builder.
		/// </summary>
		/// <remarks>
		/// Use the builder in an initialized <c>ItemStore<c> as a shortcut.
		/// </remarks>
		/// <param name="itemStore"></param>
		public InventoryFilterBuilder(ItemStore itemStore)
		{
			this.itemStore = itemStore ?? throw new ArgumentNullException(nameof(itemStore));
			
			propertyFilterBuilder = new PropertyFilterBuilder(itemStore);
		}
		
		public InventoryFilterBuilder WithLimitOfZero() => WithLimitOf(0);
		
		public InventoryFilterBuilder WithLimitOf(int limit)
		{
			if (limit < 0) throw new ArgumentOutOfRangeException(nameof(limit), "Must be greater than or equal to zero.");

			this.limit = limit;

			return this;
		}
		
		public InventoryFilterBuilder RequireAll(params PropertyValidation[] validations)
		{
			propertyFilterBuilder.RequireAll(validations);
			return this;
		}
		
		public InventoryFilterBuilder RequireNone(params PropertyValidation[] validations)
		{
			propertyFilterBuilder.RequireNone(validations);
			return this;
		}
		
		public InventoryFilterBuilder RequireAny(params PropertyValidation[] validations)
		{
			propertyFilterBuilder.RequireAny(validations);
			return this;
		}

		public InventoryFilter Done()
		{
			if (itemStore == null) throw new ArgumentNullException(nameof(itemStore));

			return new InventoryFilter(
				propertyFilterBuilder.Done(),
				limit
			);
		}

		public static implicit operator InventoryFilter(InventoryFilterBuilder builder) => builder.Done();
	}
}