namespace Lunra.Hothouse.Models
{
	public static class PrefabTagCategories
	{
		static bool CategoryContains(string tagPrefix, string prefabTag) => !string.IsNullOrEmpty(prefabTag) && prefabTag.StartsWith(tagPrefix);
		
		public static class Room
		{
			const string TagPrefix = "room_";
			static string CreateTag(string prefabTag) => TagPrefix + prefabTag;
			
			// public static bool Contains(string prefabTag) => CategoryContains(TagPrefix, prefabTag);

			public static readonly string Spawn = CreateTag("spawn");
		}
	}
}