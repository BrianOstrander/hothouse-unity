using System;
using System.Collections.Generic;

namespace Lunra.Satchel
{
	public class ContainerFilterBuilder
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
		public ContainerFilterBuilder(ItemStore itemStore)
		{
			this.itemStore = itemStore ?? throw new ArgumentNullException(nameof(itemStore));
			
			propertyFilterBuilder = new PropertyFilterBuilder(itemStore);
		}
		
		public ContainerFilterBuilder WithLimitOfZero() => WithLimitOf(0);
		
		public ContainerFilterBuilder WithLimitOf(int limit)
		{
			if (limit < 0) throw new ArgumentOutOfRangeException(nameof(limit), "Must be greater than or equal to zero.");

			this.limit = limit;

			return this;
		}
		
		public ContainerFilterBuilder RequireAll(params PropertyValidation[] validations)
		{
			propertyFilterBuilder.RequireAll(validations);
			return this;
		}
		
		public ContainerFilterBuilder RequireNone(params PropertyValidation[] validations)
		{
			propertyFilterBuilder.RequireNone(validations);
			return this;
		}
		
		public ContainerFilterBuilder RequireAny(params PropertyValidation[] validations)
		{
			propertyFilterBuilder.RequireAny(validations);
			return this;
		}

		public ContainerFilter Done()
		{
			if (itemStore == null) throw new ArgumentNullException(nameof(itemStore));

			return new ContainerFilter(
				propertyFilterBuilder.Done(),
				limit
			);
		}

		public static implicit operator ContainerFilter(ContainerFilterBuilder builder) => builder.Done();
	}
}