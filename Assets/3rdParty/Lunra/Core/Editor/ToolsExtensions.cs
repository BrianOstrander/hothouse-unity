using UnityEditor;
using UnityEngine;

namespace Lunra.Editor.Core
{
	public static class ToolsExtensions
	{
		public static bool IsLayerVisible(int layer) => IsLayerVisible(LayerMask.LayerToName(layer));
		public static bool IsLayerVisible(string layerName)
		{
			var mask = LayerMask.GetMask(layerName);
			return (Tools.visibleLayers & mask) == mask;
		}
	}
}