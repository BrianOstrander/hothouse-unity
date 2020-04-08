using System;
using UnityEditor;

namespace LunraGamesEditor
{
	public class CoreAssetPostprocessor : AssetPostprocessor 
	{
		public static event Action OnPostprocessAllAssetsEvents;

		static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
		{
			if (OnPostprocessAllAssetsEvents != null) OnPostprocessAllAssetsEvents.Invoke();
		}
	}
}