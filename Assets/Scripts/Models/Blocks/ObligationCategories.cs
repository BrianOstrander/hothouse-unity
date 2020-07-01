namespace Lunra.Hothouse.Models
{
	public static class ObligationCategories
	{
		public static class Door
		{
			const string Category = "door_";

			public static Obligation Open => Obligation.New(Category + "open");
		}
	}
}