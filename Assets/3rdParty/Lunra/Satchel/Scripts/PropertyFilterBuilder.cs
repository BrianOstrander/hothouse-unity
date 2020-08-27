using System;
using System.Collections.Generic;

namespace Lunra.Satchel
{
	public class PropertyFilterBuilder
	{
		ItemStore itemStore;
		List<PropertyValidation> all = new List<PropertyValidation>();
		List<PropertyValidation> none = new List<PropertyValidation>();
		List<PropertyValidation> any = new List<PropertyValidation>();

		public PropertyFilterBuilder(ItemStore itemStore)
		{
			this.itemStore = itemStore ?? throw new ArgumentNullException(nameof(itemStore));
		}
		
		public PropertyFilterBuilder RequireAll(params PropertyValidation[] validations)
		{
			all.AddRange(validations);
			return this;
		}
		
		public PropertyFilterBuilder RequireNone(params PropertyValidation[] validations)
		{
			none.AddRange(validations);
			return this;
		}
		
		public PropertyFilterBuilder RequireAny(params PropertyValidation[] validations)
		{
			any.AddRange(validations);
			return this;
		}

		public PropertyFilter Done()
		{
			if (itemStore == null) throw new ArgumentNullException(nameof(itemStore));

			return new PropertyFilter(
				all.ToArray(),
				none.ToArray(),
				any.ToArray()
			).Initialize(itemStore);
		}

		public static implicit operator PropertyFilter(PropertyFilterBuilder builder) => builder.Done();
	}
}