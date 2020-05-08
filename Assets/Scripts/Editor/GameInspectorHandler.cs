using System.Linq;
using Lunra.Core;
using Lunra.StyxMvp;
using Lunra.StyxMvp.Services;
using Lunra.WildVacuum.Editor;
using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Services;
using UnityEditor;
using UnityEngine;

namespace Lunra.Editor.Core
{
	[InitializeOnLoad]
	public static class GameInspectorHandler
	{
		static GameState current;
		
		static GameInspectorHandler()
		{
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
			SceneView.duringSceneGui += OnDuringSceneGui;
		}

		static void OnPlayModeStateChanged(PlayModeStateChange playModeState)
		{
			switch (playModeState)
			{
				case PlayModeStateChange.ExitingEditMode:
				case PlayModeStateChange.ExitingPlayMode:
					current = null;
					break;
			}
		}

		static void OnDuringSceneGui(SceneView sceneView)
		{
			if (!GameInspectionSettings.IsInspecting.Value) return;
			if (!Application.isPlaying || !App.HasInstance || App.S == null) return;
			if (!App.S.Is(typeof(GameState), StateMachine.Events.Idle)) return;
			
			if (current == null) current = App.S.CurrentHandler as GameState;

			if (GameInspectionSettings.IsInspectingBuildings.Value)
			{
				foreach (var model in current.Payload.Game.Buildings.AllActive)
				{
					var label = "Id: " + StringExtensions.GetNonNullOrEmpty(model.Id.Value, "< null or empty Id >");

					label += GetInventory(model.Inventory.Value);

					if (model.DesireQuality.Value.Any())
					{
						label += "\nDesires:";
						foreach (var desireQuality in model.DesireQuality.Value)
						{
							label += "\n  " + desireQuality.Desire + " : " + desireQuality.Quality.ToString("N1") + " - " + desireQuality.State;
						}
					}

					Handles.Label(
						model.Position.Value,
						StringExtensions.Wrap(label, "<color=cyan>", "</color>")
					);
				}
			}

			if (GameInspectionSettings.IsInspectingDwellers.Value)
			{
				foreach (var model in current.Payload.Game.Dwellers.AllActive)
				{
					var label = "Id: " + StringExtensions.GetNonNullOrEmpty(model.Id.Value, "< null or empty Id >");

					label += "\nState: " + model.Context.CurrentState;
					
					if (model.Job.Value != Jobs.None) label += "\nJob: " + model.Job.Value + "_" + model.JobPriority.Value;
					if (model.Desire.Value != Desires.None) label += "\nDesire: " + model.Desire.Value;

					label += GetInventory(model.Inventory.Value);

					Handles.Label(
						model.Position.Value + (Vector3.up * 3f),
						StringExtensions.Wrap(label, "<color=cyan>", "</color>")
					);
				}
			}
		}

		public static string GetInventory(Inventory inventory)
		{
			var result = string.Empty;
			
			if (!inventory.IsCapacityZero)
			{
				result += "\nInventory:";
				foreach (var maximum in inventory.Maximum.Where(i => 0 < i.Count))
				{
					result += "\n  " + maximum.Type + " : " + inventory[maximum.Type] + " / " + maximum.Count;
				}
			}

			return result;
		}
	}
}