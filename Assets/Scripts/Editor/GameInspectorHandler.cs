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
		enum InventoryVisibilities
		{
			Unknown = 0,
			Always = 10,
			IfNotEmpty = 20,
			IfMaximumGreaterThanZero = 30,
			IfNotFull = 40
		}
		
		static GameState current;
		static GUIStyle labelStyle;
		
		static GameInspectorHandler()
		{
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
			SceneView.duringSceneGui += OnDuringSceneGui;
		}

		static void OnPlayModeStateChanged(PlayModeStateChange playModeState)
		{
			switch (playModeState)
			{
				case PlayModeStateChange.EnteredPlayMode:
					labelStyle = new GUIStyle(EditorStyles.label);
					labelStyle.richText = true;
					break;
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

					if (model.BuildingState.Value != BuildingStates.Operating)
					{
						label += "\nState: " + model.BuildingState.Value;
					}
					
					label += GetInventory(
						"Inventory",
						model.Inventory.Value,
						model.InventoryCapacity.Value
					);

					switch (model.BuildingState.Value)
					{
						case BuildingStates.Constructing:
							label += GetInventory(
								"Construction",
								model.ConstructionInventory.Value,
								model.ConstructionInventoryCapacity.Value,
								InventoryVisibilities.IfNotFull
							);
							
							label += GetInventory(
								"Construction Promised",
								model.ConstructionInventoryPromised.Value
							);
							break;
					}

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

					if (model.BuildingState.Value == BuildingStates.Constructing)
					{
						Handles.color = Color.yellow.NewA(0.2f);
						Handles.DrawWireCube(model.Position.Value, Vector3.one);
					}
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

					label += GetInventory(
						"Inventory",
						model.Inventory.Value,
						model.InventoryCapacity.Value,
						InventoryVisibilities.IfMaximumGreaterThanZero
						
					);

					if (model.InventoryPromise.Value.Operation != InventoryPromise.Operations.None)
					{
						label += GetInventory(
							model.InventoryPromise.Value.Operation+"Promise",
							model.InventoryPromise.Value.Inventory
						);
					}

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

			if (GameInspectionSettings.IsInspectingFlora.Value)
			{
				foreach (var model in current.Payload.Game.Flora.AllActive)
				{
					if (model.IsReproducing.Value) continue;
					Handles.color = Color.red;
					Handles.DrawWireCube(model.Position.Value, Vector3.one);
				}
			}
		}

		static string GetInventory(
			string label,
			Inventory inventory,
			InventoryCapacity? capacity = null,
			InventoryVisibilities inventoryVisibilities = InventoryVisibilities.IfNotEmpty
		)
		{
			var result = string.Empty;

			foreach (var type in EnumExtensions.GetValues(Item.Types.Unknown))
			{
				var value = inventory[type];
				var valueMaximum = capacity?.GetMaximumFor(inventory, type);
				
				switch (inventoryVisibilities)
				{
					case InventoryVisibilities.Always:
						break;
					case InventoryVisibilities.IfNotEmpty:
						if (value == 0) continue;
						break;
					case InventoryVisibilities.IfMaximumGreaterThanZero:
						if ((!valueMaximum.HasValue || valueMaximum.Value == 0) && value == 0) continue;
						break;
					case InventoryVisibilities.IfNotFull:
						if (valueMaximum.HasValue && valueMaximum.Value == value) continue;
						break;
				}
				
				result += "\n  " + type + " : " + value + (valueMaximum.HasValue ? (" / " + valueMaximum.Value) : string.Empty);
			}

			return string.IsNullOrEmpty(result) ? result : ("\n" + label + ":" + result);
		}

		public static void OpenHandlerAsset()
		{
			AssetDatabase.OpenAsset(
				AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/Scripts/Editor/" + nameof(GameInspectorHandler) + ".cs"),
				40
			);
		}
	}
}