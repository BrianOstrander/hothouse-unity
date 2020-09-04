using System;
using System.Collections.Generic;
using System.Linq;

namespace Lunra.Satchel
{
	public class ContainerConstraintBuilder
	{
		ItemStore itemStore;
		int limit = int.MaxValue;
		int limitDefault = int.MaxValue;
		List<ContainerFilter> restrictions = new List<ContainerFilter>();

		public ContainerConstraintBuilder(ItemStore itemStore)
		{
			this.itemStore = itemStore ?? throw new ArgumentNullException(nameof(itemStore));
		}
		
		public ContainerConstraintBuilder WithLimitOf(int limit)
		{
			this.limit = limit;
			return this;
		}

		public ContainerConstraintBuilder WithLimitDefaultOf(int limitDefault)
		{
			this.limitDefault = limitDefault;
			return this;
		}

		public ContainerConstraintBuilder WithLimitOfZero() => WithLimitOf(0);
		public ContainerConstraintBuilder WithLimitDefaultOfZero() => WithLimitDefaultOf(0);

		public ContainerConstraintBuilder Restrict(
			ContainerFilter containerFilter
		)
		{
			restrictions.Add(containerFilter);

			return this;
		}

		public ContainerConstraint Done()
		{
			if (itemStore == null) throw new ArgumentNullException(nameof(itemStore));

			return new ContainerConstraint(
				limit,
				limitDefault,
				restrictions.ToArray()
			).Initialize(itemStore);
		}

		public static implicit operator ContainerConstraint(ContainerConstraintBuilder builder) => builder.Done();
	}
}