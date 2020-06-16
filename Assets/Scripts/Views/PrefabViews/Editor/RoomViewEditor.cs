using System.Collections.Generic;
using Lunra.Editor.Core;
using Lunra.Hothouse.Models;
using Lunra.NumberDemon;
using UnityEditor;
using UnityEngine;

namespace Lunra.Hothouse.Views.Editor
{
	[CustomEditor(typeof(RoomView))]
	public class DrawLineEditor : UnityEditor.Editor
	{
		int lastFocusId;
		bool isRefocusing;
		// string last
		List<string> issues = new List<string>();
		
		void OnSceneGUI()
		{
			var typedTarget = target as RoomView;
			if (typedTarget == null)
			{
				isRefocusing = true;
				return;
			}

			if (isRefocusing)
			{
				isRefocusing = false;
				if (lastFocusId != typedTarget.gameObject.GetInstanceID()) CalculateIssues();
				lastFocusId = typedTarget.gameObject.GetInstanceID();
			}
			
			Handles.BeginGUI();
			{
				var isUnexploredLayerVisible = ToolsExtensions.IsLayerVisible(LayerNames.Unexplored);
				
				if (GUILayout.Button("Unexplored <b>" + (isUnexploredLayerVisible ? "Visible" : "Hidden") + "</b>", EditorStylesExtensions.ButtonRichText, GUILayout.ExpandWidth(false)))
				{
					Tools.visibleLayers ^= LayerMask.GetMask(LayerNames.Unexplored);
				}
				
				GUIExtensions.PushEnabled(!Application.isPlaying);
				{
					if (GUILayout.Button("Recache", GUILayout.ExpandWidth(false)))
					{
						typedTarget.CalculateCachedData();
					}
				}
				GUIExtensions.PopEnabled();

				GUIExtensions.PushEnabled(Application.isPlaying);
				{
					if (GUILayout.Button("Test Collision", GUILayout.ExpandWidth(false)))
					{
						for (var i = 0; i < 1000f; i++)
						{
							var testPosition = typedTarget.transform.position + Vector3.up;
							var testDir = new Vector3(
								DemonUtility.GetNextFloat(-1f, 1f),
								DemonUtility.GetNextFloat(-1f, 1f),
								DemonUtility.GetNextFloat(-1f, 1f)
							).normalized;

							testDir *= DemonUtility.GetNextFloat(1f, 30f);

							testPosition += testDir;

							if (typedTarget.BoundaryContains(testPosition))
							{
								Debug.DrawLine(
									testPosition,
									testPosition + (Vector3.up * 0.1f),
									Color.green,
									3f
								);
							}
							else
							{
								Debug.DrawLine(
									testPosition,
									testPosition + (Vector3.up * 0.1f),
									Color.red,
									3f
								);
							}
						}
					}
				}
				GUIExtensions.PopEnabled();
			}
			Handles.EndGUI();
		}

		void CalculateIssues()
		{
			issues.Clear();
			
			
		}

		void OnValidateGui()
		{
			
			
			
		}
	}
}