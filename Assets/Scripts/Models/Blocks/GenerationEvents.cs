namespace Lunra.Hothouse.Models
{
	public static class GenerationEvents
	{
		public const string Begin = "generation_begin";
		public const string End = "generation_end";

		public const string RoomGenerationBegin = "generation_room_generation_begin";
		public const string RoomGenerationEnd = "generation_room_generation_end";
		
		public const string SpawnChosen = "generation_spawn_chosen";
		
		public const string CalculateNavigationBegin = "generation_calculate_navigation_begin";
		public const string CalculateNavigationEnd = "generation_calculate_navigation_end";
		
		public const string FloraSeedAppend = "generation_flora_seed_append";
		public const string FloraClusterAppend = "generation_flora_cluster_append";
	}
}