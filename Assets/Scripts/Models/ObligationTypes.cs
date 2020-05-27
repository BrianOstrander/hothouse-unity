namespace Lunra.Hothouse.Models
{
	public static class ObligationTypes
	{
		public static Jobs[] GetJobs(params Jobs[] jobs) => jobs;
		
		public static class Door
		{
			const string Prefix = "door_";

			public const string Open = Prefix + "open";
		}
	}
}