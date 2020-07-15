namespace Lunra.Hothouse.Models
{
	public static class BuildingTypes
	{
		public static class Stockpiles
		{
			public const string StartingWagon = "stockpile_starting_wagon";
			public const string SmallDepot = "stockpile_small_depot";
		}

		public static class Lights
		{
			public const string Bonfire = "light_bonfire";
		}

		public static class Beds
		{
			public const string Bedroll = "bed_bedroll";
		}
		
		public static class Barricades
		{
			public const string Small = "barricade_small";
		}

		public static readonly string[] All =
		{
			Stockpiles.StartingWagon,
			Stockpiles.SmallDepot,
			
			Lights.Bonfire,
			
			Beds.Bedroll,
			
			Barricades.Small
		};
	}
}