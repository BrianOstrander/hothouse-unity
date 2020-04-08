using UnityEditor;
using LunraGamesEditor;

namespace LunraGames.SubLight
{
	public static class ScriptableSingletonsMenu
	{
		[MenuItem("Assets/Create/Singletons/Default Views")]
		static void CreateDefaultViews()
		{
			AssetDatabaseExtensions.CreateObject<DefaultViews>("DefaultViews");
		}
	}
}