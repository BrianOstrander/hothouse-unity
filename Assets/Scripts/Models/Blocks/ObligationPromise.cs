using System;
using Lunra.StyxMvp.Models;

namespace Lunra.Hothouse.Models
{
	public class ObligationPromise
	{
		public static ObligationPromise New(
			string type,
			IModel target
		)
		{
			return new ObligationPromise(
				type,
				InstanceId.New(target)
			);
		}
		
		public string Type { get; }
		public InstanceId Target { get; }

		ObligationPromise(
			string type,
			InstanceId target
		)
		{
			if (target.IsNull) throw new ArgumentException("Cannot have a null instance: "+target);

			Type = type;
			Target = target;
		}
	}
}