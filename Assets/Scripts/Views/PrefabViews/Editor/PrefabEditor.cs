using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.Editor.Core;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Lunra.Hothouse.Views.Editor
{
	public abstract class PrefabEditor<T> : UnityEditor.Editor
		where T : Component, IPrefabView
	{
		protected static readonly GUILayoutOption DefaultWidth = GUILayout.Width(160f);
	
		public override void OnInspectorGUI()
		{
			if (targets == null || targets.Length == 1) DrawInspectorSingle();
			else DrawInspectorMultiple();
		}

		public void OnSceneGUI()
		{
			var typedTarget = target as T;
			if (typedTarget == null) return;
			
			Handles.BeginGUI();
			{
				GUIExtensions.PushEnabled(!Application.isPlaying);
				{
					if (GUILayout.Button("Calculate Cached Data", DefaultWidth))
					{
						typedTarget.CalculateCachedData();
					}

					if (GUILayout.Button("Normalize Mesh Colliders", DefaultWidth))
					{
						typedTarget.NormalizeMeshCollidersFromRoot();
					}
				}
				GUIExtensions.PopEnabled();
			}
			Handles.EndGUI();
			
			DrawScene();
		}

		protected virtual void DrawInspectorSingle() => base.OnInspectorGUI();
		
		protected virtual void DrawInspectorMultiple()
		{
			GUIExtensions.PushEnabled(!Application.isPlaying);
			{
				if (GUILayout.Button("Batch Cache " + targets.Length + " items"))
				{
					var batchTargets = targets;

					EditorCoroutineUtility.StartCoroutine(
						BatchProcess(
							batchTargets,
							result =>
							{
								StageUtility.GoBackToPreviousStage();
								Selection.objects = batchTargets;
								result.Log();
							}
						),
						this
					);
				}
			}
			GUIExtensions.PopEnabled();
		}

		protected virtual void DrawScene() { }

		void OnProcess(
			Object batchTarget,
			Action<Result<string>> done
		)
		{
			try
			{
				AssetDatabase.OpenAsset(batchTarget);
				var stage = StageUtility.GetCurrentStage();
				var typedTarget = stage.FindComponentOfType<T>();

				typedTarget.CalculateCachedData();
				
				done(Result<string>.Success($"\n\t{typedTarget.name} - {stage.assetPath}"));
			}
			catch (Exception e)
			{
				var error = $"\n\t<color=red>{(batchTarget == null ? "null" : batchTarget.name)} ERROR";
				error += $"\n\t\tMessage: {e.Message}</color>";
				
				done(Result<string>.Failure(error));
			}	
		}

		IEnumerator BatchProcess(
			Object[] objects,
			Action<Result<string>> done
		)
		{
			var result = $"Processing {objects.Length} {typeof(T).Name} prefab(s)";

			var errorCount = 0;
			var errorResults = string.Empty;
			var successResults = string.Empty;
		
			foreach (var current in objects)
			{
				OnProcess(
					current,
					currentResult =>
					{
						if (currentResult.Status != ResultStatus.Success)
						{
							errorCount++;
							errorResults += "\n" + currentResult.Error;
						}
						else
						{
							successResults += "\n" + currentResult.Payload;
						}
					}
				);
				
				yield return null;
			}

			if (0 < errorCount) result = $"<color=red>{result} with {errorCount} errors</color>";

			result += errorResults + successResults;

			if (0 < errorCount) done(Result<string>.Failure(result));
			else done(Result<string>.Success(result));
		}
	}
}