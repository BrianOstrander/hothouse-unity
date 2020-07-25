using System;
using System.Collections;
using Lunra.Editor.Core;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Lunra.Hothouse.Views.Editor
{
	public abstract class BatchEditor<T> : UnityEditor.Editor
		where T : Component, ICachableView
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
							var typedTarget = stage.FindComponentOfType<T>();

							TriggerCalculateCache(typedTarget);

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
								StageUtility.GoBackToPreviousStage();
								Selection.objects = batchTargets;
					
								Debug.Log(batchResult);								
							}
						),
						this
					);
				}
			}
			GUIExtensions.PopEnabled();
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

		protected void TriggerCalculateCache(T typedTarget)
		{
			Undo.RecordObject(typedTarget, "Calculate Cached Data");
			typedTarget.PrefabId = typedTarget.name;
			typedTarget.CalculateCachedData();
			PrefabUtility.RecordPrefabInstancePropertyModifications(typedTarget);
		}
	}
}