namespace Lunra.Hothouse.Models
{
	public static class Modifiers
	{
		public static class Farm
		{
			const string Prefix = "farm_";

			public const string Sown = Prefix + "sown";
			public const string Tended = Prefix + "tended";
		}
		
		public static class Water
		{
			const string Prefix = "water_";

			public const string Applied = Prefix + "applied";
		}
	}
}