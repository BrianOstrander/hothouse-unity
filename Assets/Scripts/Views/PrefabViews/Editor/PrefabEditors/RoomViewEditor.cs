using System.Linq;
using Lunra.Editor.Core;
using Lunra.Hothouse.Models;
using Lunra.NumberDemon;
using UnityEditor;
using UnityEngine;

namespace Lunra.Hothouse.Views.Editor
{
	[CustomEditor(typeof(RoomView))]
	[CanEditMultipleObjects]
	public class RoomViewEditor : PrefabEditor<RoomView>
	{
		protected override void DrawScene()
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

				GUIExtensions.PushEnabled(!Application.isPlaying);
				{
					if (GUILayout.Button("Default Materials", GUILayout.ExpandWidth(false)))
					{
						typedTarget.ApplyDefaultMaterials();
					}

					if (GUILayout.Button("Is Spawn <b>" + (typedTarget.PrefabTags.Contains(PrefabTagCategories.Room.Spawn) ? "True" : "False") + "</b>", EditorStylesExtensions.ButtonRichText, GUILayout.ExpandWidth(false)))
					{
						typedTarget.ToggleSpawnTag();
					}
				}
				GUIExtensions.PopEnabled();

				GUIExtensions.PushEnabled(Application.isPlaying);
				{
					if (GUILayout.Button("Test Random Point", GUILayout.ExpandWidth(false)))
					{
						var generator = new Demon();
						
						for (var i = 0; i < 1000; i++)
						{
							var testPosition = typedTarget.BoundaryRandomPoint(generator);

							if (testPosition.HasValue)
							{
								if (typedTarget.BoundaryContains(testPosition.Value))
								{
									Debug.DrawLine(
										testPosition.Value,
										testPosition.Value + (Vector3.up * 0.1f),
										Color.green,
										3f
									);
								}
								else
								{
									Debug.DrawLine(
										testPosition.Value,
										testPosition.Value + (Vector3.up * 0.1f),
										Color.red,
										3f
									);
								}
							}
						}
					}
					
					if (GUILayout.Button("Test Collision", GUILayout.ExpandWidth(false)))
					{
						for (var i = 0; i < 1000; i++)
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
	}
}