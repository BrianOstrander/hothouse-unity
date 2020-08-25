using System;
using System.Collections.Generic;

namespace Lunra.Satchel
{
	public class InventoryFilterBuilder
	{
		ItemStore itemStore;
		int limit = int.MaxValue;
		List<PropertyValidation> all = new List<PropertyValidation>();
		List<PropertyValidation> none = new List<PropertyValidation>();
		List<PropertyValidation> any = new List<PropertyValidation>();

		public InventoryFilterBuilder(ItemStore itemStore)
		{
			this.itemStore = itemStore ?? throw new ArgumentNullException(nameof(itemStore));
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
			all.AddRange(validations);
			return this;
		}
		
		public InventoryFilterBuilder RequireNone(params PropertyValidation[] validations)
		{
			none.AddRange(validations);
			return this;
		}
		
		public InventoryFilterBuilder RequireAny(params PropertyValidation[] validations)
		{
			any.AddRange(validations);
			return this;
		}

		public InventoryFilter Done()
		{
			if (itemStore == null) throw new ArgumentNullException(nameof(itemStore));

			return new InventoryFilter(
				new PropertyFilter(all.ToArray(), none.ToArray(), any.ToArray()).Initialize(itemStore),
				limit
			);
		}

		public static implicit operator InventoryFilter(InventoryFilterBuilder builder) => builder.Done();
	}
}