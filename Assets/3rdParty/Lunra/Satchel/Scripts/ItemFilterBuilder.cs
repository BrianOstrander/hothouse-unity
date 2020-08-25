using System;
using System.Collections.Generic;

namespace Lunra.Satchel
{
	public class ItemFilterBuilder
	{
		public static ItemFilterBuilder Begin(ItemStore itemStore)
		{
			var result = new ItemFilterBuilder();
			result.itemStore = itemStore;
			return result;
		}

		ItemStore itemStore;
		List<PropertyValidation> all = new List<PropertyValidation>();
		List<PropertyValidation> none = new List<PropertyValidation>();
		List<PropertyValidation> any = new List<PropertyValidation>();

		public ItemFilterBuilder RequireAll(params PropertyValidation[] validations)
		{
			all.AddRange(validations);
			return this;
		}
		
		public ItemFilterBuilder RequireNone(params PropertyValidation[] validations)
		{
			none.AddRange(validations);
			return this;
		}
		
		public ItemFilterBuilder RequireAny(params PropertyValidation[] validations)
		{
			any.AddRange(validations);
			return this;
		}

		public ItemFilter Done()
		{
			if (itemStore == null) throw new ArgumentNullException(nameof(itemStore));

			return new ItemFilter(all.ToArray(), none.ToArray(), any.ToArray()).Initialize(itemStore);
		}

		public static implicit operator ItemFilter(ItemFilterBuilder builder) => builder.Done();
	}
}