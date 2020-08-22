using System;
using System.Collections.Generic;
using System.Linq;

namespace Lunra.Satchel
{
	public class ItemFilterBuilder
	{
		public static ItemFilterBuilder RequiringAll(params PropertyValidation[] validations)
		{
			var result = new ItemFilterBuilder();
			result.all = validations.ToList();
			return result;
		}
		
		public static ItemFilterBuilder RequiringNone(params PropertyValidation[] validations)
		{
			var result = new ItemFilterBuilder();
			result.none = validations.ToList();
			return result;
		}
		
		public static ItemFilterBuilder RequiringAny(params PropertyValidation[] validations)
		{
			var result = new ItemFilterBuilder();
			result.any = validations.ToList();
			return result;
		}
			
		List<PropertyValidation> all = new List<PropertyValidation>();
		List<PropertyValidation> none = new List<PropertyValidation>();
		List<PropertyValidation> any = new List<PropertyValidation>();

		public ItemFilterBuilder ConcatAll(params PropertyValidation[] validations)
		{
			all.AddRange(validations);
			return this;
		}
		
		public ItemFilterBuilder ConcatNone(params PropertyValidation[] validations)
		{
			none.AddRange(validations);
			return this;
		}
		
		public ItemFilterBuilder ConcatAny(params PropertyValidation[] validations)
		{
			any.AddRange(validations);
			return this;
		}

		public ItemFilter Build(ItemStore itemStore)
		{
			if (itemStore == null) throw new ArgumentNullException(nameof(itemStore));

			return new ItemFilter(all.ToArray(), none.ToArray(), any.ToArray()).Initialize(itemStore);
		}
	}
}