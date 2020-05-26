namespace Lunra.Hothouse.Models
{
	public struct ObligationPromise
	{
		public readonly string TargetId;
		public readonly string ObligationId;

		public ObligationPromise(
			string targetId,
			string obligationId
		)
		{
			TargetId = targetId;
			ObligationId = obligationId;
		}
	}
}