using System;
using System.Collections.Generic;
using System.Linq;

namespace Lunra.Satchel
{
	public class ItemConstraintBuilder
	{
		public static ItemConstraintBuilder Begin(
			ItemStore itemStore,
			int limit = int.MaxValue,
			int limitDefault = int.MaxValue
		)
		{
			var result = new ItemConstraintBuilder();
			result.itemStore = itemStore;
			result.limit = limit;
			result.limitDefault = limitDefault;
			return result;
		}

		ItemStore itemStore;
		int limit = int.MaxValue;
		int limitDefault = int.MaxValue;
		List<(int Limit, ItemFilter Filter)> restrictions = new List<(int Limit, ItemFilter Filter)>();

		public ItemConstraintBuilder WithLimitOf(int limit)
		{
			this.limit = limit;
			return this;
		}

		public ItemConstraintBuilder WithLimitDefaultOf(int limitDefault)
		{
			this.limitDefault = limitDefault;
			return this;
		}

		public ItemConstraintBuilder WithLimitOfZero() => WithLimitOf(0);
		public ItemConstraintBuilder WithLimitDefaultOfZero() => WithLimitDefaultOf(0);

		public ItemConstraintBuilder Permit(
			ItemFilter filter
		)
		{
			return Permit(
				int.MaxValue,
				filter
			);
		}
		
		public ItemConstraintBuilder Permit(
			int limit,
			ItemFilter filter
		)
		{
			return Restrict(
				limit,
				filter
			);
		}

		public ItemConstraintBuilder Forbid(
			ItemFilter filter
		)
		{
			return Restrict(
				0,
				filter
			);
		}
		
		ItemConstraintBuilder Restrict(
			int limit,
			ItemFilter filter
		)
		{
			if (limit < 0) throw new ArgumentOutOfRangeException(nameof(limit), "Must be greater than or equal to zero");
			if (filter == null) throw new ArgumentNullException(nameof(filter));
			
			restrictions.Add((limit, filter));

			return this;
		}

		public ItemConstraint Done()
		{
			if (itemStore == null) throw new ArgumentNullException(nameof(itemStore));

			return new ItemConstraint(
				limit,
				limitDefault,
				restrictions
					.Select(e => new ItemConstraint.Restriction(e.Filter, limit))
					.ToArray()
			).Initialize(itemStore);
		}

		public static implicit operator ItemConstraint(ItemConstraintBuilder builder) => builder.Done();
	}
}