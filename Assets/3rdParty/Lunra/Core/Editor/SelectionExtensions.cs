using System.IO;
using UnityEditor;

namespace LunraGamesEditor
{
	public static class SelectionExtensions
	{
		/// <summary>
		/// Returns the path of the currently selected directory, or the parent directory of a selected asset.
		/// </summary>
		public static string Directory()
		{
			var asset = Selection.activeObject;
			if (asset == null) return null;
			var containingDir = Path.GetDirectoryName(AssetDatabase.GetAssetPath(asset));
			return asset is DefaultAsset ? Path.Combine(containingDir, asset.name) : containingDir;
		}
	}
}