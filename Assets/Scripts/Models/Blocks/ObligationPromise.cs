using System;
using Lunra.StyxMvp.Models;

namespace Lunra.Hothouse.Models
{
	public class ObligationPromise
	{
		public static ObligationPromise New(
			Obligation obligation,
			IModel target
		)
		{
			return new ObligationPromise(
				obligation,
				InstanceId.New(target)
			);
		}
		
		public Obligation Obligation { get; }
		public InstanceId Target { get; }

		ObligationPromise(
			Obligation obligation,
			InstanceId target
		)
		{
			if (target.IsNull) throw new ArgumentException("Cannot have a null instance: "+target);

			Obligation = obligation;
			Target = target;
		}

		public override string ToString() => Obligation.Type + " on " + Target;
	}
}