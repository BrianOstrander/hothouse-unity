using System;
using UnityEditor;

namespace Lunra.Editor.Core
{
	public class CoreAssetPostprocessor : AssetPostprocessor 
	{
		public static event Action OnPostprocessAllAssetsEvents;

		static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
		{
			OnPostprocessAllAssetsEvents?.Invoke();
		}
	}
}