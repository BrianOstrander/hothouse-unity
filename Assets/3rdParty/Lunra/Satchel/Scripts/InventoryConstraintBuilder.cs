using System;
using System.Collections.Generic;
using System.Linq;

namespace Lunra.Satchel
{
	public class InventoryConstraintBuilder
	{
		ItemStore itemStore;
		int limit = int.MaxValue;
		int limitDefault = int.MaxValue;
		List<InventoryFilter> restrictions = new List<InventoryFilter>();

		public InventoryConstraintBuilder(ItemStore itemStore)
		{
			this.itemStore = itemStore ?? throw new ArgumentNullException(nameof(itemStore));
		}
		
		public InventoryConstraintBuilder WithLimitOf(int limit)
		{
			this.limit = limit;
			return this;
		}

		public InventoryConstraintBuilder WithLimitDefaultOf(int limitDefault)
		{
			this.limitDefault = limitDefault;
			return this;
		}

		public InventoryConstraintBuilder WithLimitOfZero() => WithLimitOf(0);
		public InventoryConstraintBuilder WithLimitDefaultOfZero() => WithLimitDefaultOf(0);

		// public ItemConstraintBuilder Permit(
		// 	ItemFilter filter
		// )
		// {
		// 	return Permit(
		// 		int.MaxValue,
		// 		filter
		// 	);
		// }
		//
		// public ItemConstraintBuilder Permit(
		// 	int limit,
		// 	ItemFilter filter
		// )
		// {
		// 	return Restrict(
		// 		limit,
		// 		filter
		// 	);
		// }
		//
		// public ItemConstraintBuilder Forbid(
		// 	ItemFilter filter
		// )
		// {
		// 	return Restrict(
		// 		0,
		// 		filter
		// 	);
		// }
		//
		// ItemConstraintBuilder Restrict(
		// 	int limit,
		// 	ItemFilter filter
		// )
		// {
		// 	if (limit < 0) throw new ArgumentOutOfRangeException(nameof(limit), "Must be greater than or equal to zero");
		// 	if (filter == null) throw new ArgumentNullException(nameof(filter));
		//
		// 	return Restrict(new ItemConstraint.Restriction(filter, limit));
		// }
		
		public InventoryConstraintBuilder Restrict(
			InventoryFilter inventoryFilter
		)
		{
			restrictions.Add(inventoryFilter);

			return this;
		}

		public InventoryConstraint Done()
		{
			if (itemStore == null) throw new ArgumentNullException(nameof(itemStore));

			return new InventoryConstraint(
				limit,
				limitDefault,
				restrictions.ToArray()
			).Initialize(itemStore);
		}

		public static implicit operator InventoryConstraint(InventoryConstraintBuilder builder) => builder.Done();
	}
}