using System.Linq;
using Lunra.Core;
using Lunra.Editor.Core;
using Lunra.StyxMvp;
using Lunra.StyxMvp.Services;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Services;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lunra.Hothouse.Editor
{
	[InitializeOnLoad]
	public static class SceneInspectionHandler
	{
		enum InventoryVisibilities
		{
			Unknown = 0,
			Always = 10,
			IfNotEmpty = 20,
			IfMaximumGreaterThanZero = 30,
			IfNotFull = 40
		}
		
		static GUIStyle labelStyle;
		
		static SceneInspectionHandler()
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
			}
		}

		static void OnDuringSceneGui(SceneView sceneView)
		{
			if (!SceneInspectionSettings.IsInspecting.Value) return;

			if (!SettingsProviderCache.GetGameState(out var gameState)) return;

			if (SceneInspectionSettings.IsInspectingBuildings.Value)
			{
				foreach (var model in gameState.Payload.Game.Buildings.AllActive)
				{
					var label = "Id: " + StringExtensions.GetNonNullOrEmpty(model.Id.Value, "< null or empty Id >");

					if (model.BuildingState.Value != BuildingStates.Operating)
					{
						label += "\nState: " + model.BuildingState.Value;
					}

					if (SceneInspectionSettings.IsInspectingLightLevels.Value)
					{
						label += "\nLight Level: " + model.LightLevel.Value.ToString("N2");
						if (model.IsLight.Value) label += "\nLight State: " + model.LightState.Value;
					}

					label += GetInventory(
						"Inventory",
						model.Inventory.Value,
						model.InventoryCapacity.Value,
						InventoryVisibilities.IfMaximumGreaterThanZero
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
						case BuildingStates.Salvaging:
							label += GetInventory(
								"Salvage",
								model.SalvageInventory.Value
							);
							break;
					}

					if (model.DesireQualities.Value.Any())
					{
						label += "\nDesires:";
						foreach (var desireQuality in model.DesireQualities.Value)
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

			if (SceneInspectionSettings.IsInspectingDwellers.Value)
			{
				foreach (var model in gameState.Payload.Game.Dwellers.AllActive)
				{
					var label = "Id: " + StringExtensions.GetNonNullOrEmpty(model.Id.Value, "< null or empty Id >");

					if (!Mathf.Approximately(model.Health.Value, model.HealthMaximum.Value)) label += "\nHealth: " + model.Health.Value.ToString("N1") + " / " + model.HealthMaximum.Value.ToString("N1");
					
					label += "\nState: " + model.Context.CurrentState;

					if (model.Job.Value != Jobs.None) label += "\nJob: " + model.Job.Value;
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
			
			if (SceneInspectionSettings.IsInspectingItemDrops.Value)
			{
				foreach (var model in gameState.Payload.Game.ItemDrops.AllActive)
				{
					var label = "Id: " + StringExtensions.GetNonNullOrEmpty(model.Id.Value, "< null or empty Id >");
					
					if (model.Job.Value != Jobs.None) label += "\nJob: " + model.Job.Value;
					
					label += GetInventory(
						"Inventory",
						model.Inventory.Value
					);
					
					label += GetInventory(
						"Cleanup Promised",
						model.WithdrawalInventoryPromised.Value
					);

					Handles.Label(
						model.Position.Value + (Vector3.up * 1f),
						StringExtensions.Wrap(label, "<color=cyan>", "</color>"),
						labelStyle
					);
				}
			}

			if (SceneInspectionSettings.IsInspectingFlora.Value)
			{
				foreach (var model in gameState.Payload.Game.Flora.AllActive)
				{
					if (model.IsReproducing.Value) continue;
					Handles.color = Color.red;
					Handles.DrawWireCube(model.Position.Value, Vector3.one);
				}
			}

			if (SceneInspectionSettings.IsInspectingLightLevels.Value)
			{
				Handles.color = Color.yellow.NewA(0.05f);
				HandlesExtensions.BeginDepthCheck(CompareFunction.Less);
				{
					foreach (var model in gameState.Payload.Game.Lights.Where(l => l.IsLightActive()))
					{
						Handles.DrawSolidDisc(
							model.Position.Value,
							Vector3.up,
							model.LightRange.Value
						);
					}
				}
				HandlesExtensions.EndDepthCheck();

				var lightSensitiveOffset = Vector3.up * 4f;
				foreach (var model in gameState.Payload.Game.LightSensitives)
				{
					Debug.DrawLine(
						model.Position.Value + lightSensitiveOffset,
						model.Position.Value + lightSensitiveOffset + (Vector3.up * model.LightLevel.Value),
						Color.yellow
					);
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

			foreach (var type in EnumExtensions.GetValues(Inventory.Types.Unknown))
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
				AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/Scripts/Editor/" + nameof(SceneInspectionHandler) + ".cs"),
				40
			);
		}
	}
}