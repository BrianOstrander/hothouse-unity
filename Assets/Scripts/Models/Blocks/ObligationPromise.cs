using Lunra.StyxMvp.Models;

namespace Lunra.Hothouse.Models
{
	public class ObligationPromise
	{
		public static ObligationPromise Default() => new ObligationPromise(false, null, null);

		public static ObligationPromise New(
			IModel target,
			string obligationPromiseId
		)
		{
			return new ObligationPromise(
				true,
				target,
				obligationPromiseId
			);
		}
		
		public bool IsEnabled { get; }
		public InstanceId TargetId { get; }
		public string ObligationPromiseId { get; }

		ObligationPromise(
			bool isEnabled,
			IModel target,
			string obligationPromiseId
		)
		{
			IsEnabled = isEnabled;
			TargetId = target == null ? InstanceId.Null() : InstanceId.New(target);
			ObligationPromiseId = obligationPromiseId;
		}
	}
}