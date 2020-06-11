using Lunra.Editor.Core;
using Lunra.Hothouse.Models;
using UnityEditor;
using UnityEngine;

namespace Lunra.Hothouse.Views.Editor
{
	[CustomEditor(typeof(RoomView))]
	public class DrawLineEditor : UnityEditor.Editor
	{
		void OnSceneGUI()
		{
			var typedTarget = target as RoomView;
			if (typedTarget == null) return;
			
			Handles.BeginGUI();
			{
				var isUnexploredLayerVisible = ToolsExtensions.IsLayerVisible(LayerNames.Unexplored);
				
				if (GUILayout.Button("Unexplored <b>" + (isUnexploredLayerVisible ? "Visible" : "Hidden") + "</b>", EditorStylesExtensions.ButtonRichText, GUILayout.ExpandWidth(false)))
				{
					Tools.visibleLayers ^= LayerMask.GetMask(LayerNames.Unexplored);
				}
				
				// if (GUILayout.Button("Unexplored: "+()))
				// Tools.visibleLayers = LayerMasks.Floor
				if (GUILayout.Button("Recache", GUILayout.ExpandWidth(false)))
				{
					typedTarget.CalculateCachedData();
				}
			}
			Handles.EndGUI();
		}
	}
}