using UnityEditor;

namespace LunraGamesEditor
{
	public static class PluginGuard
	{
		static bool? _IsInPluginsDirectory;

		public static bool IsInPluginsDirectory
		{ 
			get 
			{
				if (_IsInPluginsDirectory.HasValue) return _IsInPluginsDirectory.Value;

				var assets = AssetDatabase.FindAssets("t:Script LunraGamesPluginGuard");
				if (assets == null || assets.Length == 0) return false;
				var path = AssetDatabase.GUIDToAssetPath(assets[0]);
				return (_IsInPluginsDirectory = path.ToLower().Contains("/plugins/")).Value;
			} 
		}
	}
}