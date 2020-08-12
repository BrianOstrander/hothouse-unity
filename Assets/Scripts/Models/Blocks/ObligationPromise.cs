using System;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;

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
		
		[JsonProperty] public Obligation Obligation { get; private set; }
		[JsonProperty] public InstanceId Target { get; private set; }

		[JsonConstructor]
		ObligationPromise() {}
		
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