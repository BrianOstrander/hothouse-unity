using System;
using System.Collections;
using System.Linq;
using Lunra.Editor.Core;
using Lunra.Hothouse.Models;
using Lunra.NumberDemon;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Lunra.Hothouse.Views.Editor
{
	[CustomEditor(typeof(RoomView))]
	[CanEditMultipleObjects]
	public class RoomViewEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			if (targets == null || targets.Length == 1)
			{
				base.OnInspectorGUI();
				return;
			}

			GUIExtensions.PushEnabled(!Application.isPlaying);
			{
				if (GUILayout.Button("Batch Cache " + targets.Length + " items"))
				{
					var batchTargets = targets;
					var batchResult = "Batch Caching " + batchTargets.Length + " Objects...";

					void onProcess(Object batchTarget)
					{
						try
						{
							AssetDatabase.OpenAsset(batchTarget);
							var stage = StageUtility.GetCurrentStage();
							var typedTarget = stage.FindComponentOfType<RoomView>();

							typedTarget.CalculateCachedData();

							batchResult += $"\n\t{typedTarget.name} - {stage.assetPath}";
						}
						catch (Exception e)
						{
							batchResult += $"\n\t<color=red>{batchTarget?.name} ERROR";
							batchResult += $"\n\t\tMessage: {e.Message}</color>";
						}
					}

					EditorCoroutineUtility.StartCoroutine(
						BatchProcess(
							batchTargets,
							onProcess,
							() =>
							{
								// StageUtility.GoBackToPreviousStage();
								// Selection.objects = batchTargets;
					
								Debug.Log(batchResult);								
							}
						),
						this
					);
				}
			}
			GUIExtensions.PopEnabled();
		}

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

				GUIExtensions.PushEnabled(!Application.isPlaying);
				{
					if (GUILayout.Button("Recache", GUILayout.ExpandWidth(false)))
					{
						typedTarget.CalculateCachedData();
					}
					
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
		
		IEnumerator BatchProcess(
			Object[] objects,
			Action<Object> process,
			Action done = null
		)
		{
			foreach (var current in objects)
			{
				process(current);
				yield return null;
			}
			
			done?.Invoke();
		}
	}
}