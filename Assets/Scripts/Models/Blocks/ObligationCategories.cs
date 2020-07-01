namespace Lunra.Hothouse.Models
{
	public static class ObligationCategories
	{
		public static class Door
		{
			const string Category = "door_";

			public static Obligation Open => Obligation.New(Category + "open");
		}

		public static class Attack
		{
			const string Category = "attack_";
			
			public static Obligation Melee => Obligation.New(Category + "melee");
		}
	}
}