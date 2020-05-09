using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.StyxMvp;
using Lunra.StyxMvp.Services;
using Lunra.Hothouse.Editor;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Services;
using UnityEditor;
using UnityEngine;

namespace Lunra.Editor.Core
{
	[InitializeOnLoad]
	public static class GameInspectorHandler
	{
		static GameState current;
		static GUIStyle labelStyle;
		
		static GameInspectorHandler()
		{
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
			SceneView.duringSceneGui += OnDuringSceneGui;
		}

		static void OnPlayModeStateChanged(PlayModeStateChange playModeState)
		{
			labelStyle = new GUIStyle(EditorStyles.label);
			labelStyle.richText = true;
			
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
						StringExtensions.Wrap(label, "<color=cyan>", "</color>"),
						labelStyle
					);
				}
			}

			if (GameInspectionSettings.IsInspectingDwellers.Value)
			{
				foreach (var model in current.Payload.Game.Dwellers.AllActive)
				{
					var label = "Id: " + StringExtensions.GetNonNullOrEmpty(model.Id.Value, "< null or empty Id >");

					if (!Mathf.Approximately(model.Health.Value, model.HealthMaximum.Value)) label += "\nHealth: " + model.Health.Value.ToString("N1") + " / " + model.HealthMaximum.Value.ToString("N1");
					
					label += "\nState: " + model.Context.CurrentState;
					
					if (model.Job.Value != Jobs.None) label += "\nJob: " + model.Job.Value + "_" + model.JobPriority.Value;
					if (model.Desire.Value != Desires.None) label += "\nDesire: " + model.Desire.Value;

					label += GetInventory(model.Inventory.Value);

					Handles.Label(
						model.Position.Value + (Vector3.up * 3f),
						StringExtensions.Wrap(label, "<color=cyan>", "</color>"),
						labelStyle
					);
					
					switch (model.NavigationPlan.Value.State)
					{
						case NavigationPlan.States.Navigating:
							var nodes = model.NavigationPlan.Value.Nodes;
							for (var i = 1; i < nodes.Length; i++) Debug.DrawLine(nodes[i - 1], nodes[i], Color.green);
							break;
						case NavigationPlan.States.Invalid:
							Debug.DrawLine(model.NavigationPlan.Value.Position, model.NavigationPlan.Value.EndPosition, Color.red);
							break;
					}
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