namespace Lunra.Hothouse.Models
{
	public static class ObligationCategories
	{
		public static class Door
		{
			const string Category = "door_";

			public static Obligation Open => Obligation.New(Category + "open");
		}

		public static class Destroy
		{
			const string Category = "destroy_";
			
			public static Obligation Melee => Obligation.New(Category + "melee");
		}

		public static class Construct
		{
			const string Category = "construct_";

			public static Obligation Assemble => Obligation.New(Category + "assemble");
		}
	}
}