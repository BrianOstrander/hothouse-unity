using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.Editor.Core;
using Lunra.StyxMvp;
using Lunra.StyxMvp.Services;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Services;
using Lunra.StyxMvp.Models;
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

			var obligationIdsHandled = new List<string>();

			string GetId(IModel model) => "Id: " + model.ShortId;
			
			bool AppendObligations(ref string label, IObligationModel model)
			{
				if (obligationIdsHandled.Contains(model.Id.Value)) return false;
				
				obligationIdsHandled.Add(model.Id.Value);
				
				label += "\nObligations:";
				if (model.Obligations.All.Value.None())
				{
					label += " None";
				}
				else
				{
					foreach (var obligation in model.Obligations.All.Value)
					{
						label += "\n  [ " + Model.ShortenId(obligation.PromiseId) + " ] " + obligation.Type + "." + obligation.State + " #" + obligation.Priority + " : " + obligation.ConcentrationRequirement + "( " + obligation.ConcentrationElapsed.Current + " / " + obligation.ConcentrationElapsed.Maximum + " )";
					}
				}

				return true;
			}
			

			if (SceneInspectionSettings.IsInspectingBuildings.Value)
			{
				foreach (var model in gameState.Payload.Game.Buildings.AllActive)
				{
					var label = GetId(model);

					if (model.BuildingState.Value != BuildingStates.Operating)
					{
						label += "\nState: " + model.BuildingState.Value;
					}
					
					if (SceneInspectionSettings.IsInspectingRooms.Value)
					{
						label += "\nRoomId: " + model.RoomTransform.Id.Value;
					}

					if (model.Health.IsDamaged)
					{
						label += "\nHealth: " + model.Health.Current.Value + " / " + model.Health.Maximum.Value;
						if (model.Health.IsDead) label += " - " + StringExtensions.Wrap("Dead", "<color=red>", "</color>");
					}

					if (SceneInspectionSettings.IsInspectingLightLevels.Value)
					{
						label += "\nLight Level: " + model.LightSensitive.LightLevel.Value.ToString("N2");
						if (model.Light.IsLight.Value) label += "\nLight State: " + model.Light.LightState.Value;
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
						model.Transform.Position.Value,
						StringExtensions.Wrap(label, "<color=cyan>", "</color>"),
						labelStyle
					);

					if (model.BuildingState.Value == BuildingStates.Constructing)
					{
						Handles.color = Color.yellow.NewA(0.2f);
						Handles.DrawWireCube(model.Transform.Position.Value, Vector3.one);
					}
				}
			}

			if (SceneInspectionSettings.IsInspectingEntrances.Value)
			{
				HandlesExtensions.BeginDepthCheck(CompareFunction.Less);
				{
					foreach (var model in gameState.Payload.Game.GetEnterables())
					{
						foreach (var entrance in model.Enterable.Entrances.Value)
						{
							var color = Color.grey;

							switch (entrance.State)
							{
								case Entrance.States.Available:
									color = Color.green;
									break;
								case Entrance.States.NotAvailable:
									color = entrance.IsNavigable ? Color.yellow : Color.red;
									break;
							}

							Handles.color = color;
							Handles.DrawDottedLine(
								model.Transform.Position.Value,
								entrance.Position,
								4f
							);
							Handles.DrawWireCube(
								entrance.Position,
								Vector3.one * 0.1f
							);
						}
					}
				}
				HandlesExtensions.EndDepthCheck();
			}

			if (SceneInspectionSettings.IsInspectingDwellers.Value)
			{
				foreach (var model in gameState.Payload.Game.Dwellers.AllActive)
				{
					var label = GetId(model);

					if (!Mathf.Approximately(model.Health.Current.Value, model.Health.Maximum.Value)) label += "\nHealth: " + model.Health.Current.Value.ToString("N1") + " / " + model.Health.Maximum.Value.ToString("N1");
					
					label += "\nState: " + model.Context.CurrentState;

					if (model.Job.Value != Jobs.None) label += "\nJob: " + model.Job.Value;
					if (model.Desire.Value != Desires.None) label += "\nDesire: " + model.Desire.Value;

					if (SceneInspectionSettings.IsInspectingObligations.Value)
					{
						label += "\nObligationPromise: ";

						if (model.Obligation.Value.IsEnabled)
						{
							var obligation = gameState.Payload.Game.GetObligations()
								.GetIndividualObligations(mo => mo.Model.Id.Value == model.Obligation.Value.TargetId && mo.Obligation.PromiseId == model.Obligation.Value.ObligationPromiseId)
								.FirstOrDefault();

							if (obligation.Model == null)
							{
								label += "Missing ObligationId \"" + Model.ShortenId(model.Obligation.Value.ObligationPromiseId) + "\" or TargetId \"" + Model.ShortenId(model.Obligation.Value.TargetId) + "\"";
							}
							else
							{
								label += Model.ShortenId(model.Obligation.Value.TargetId) + "[ " + Model.ShortenId(model.Obligation.Value.ObligationPromiseId) + " ]." + obligation.Obligation.Type;
							}
						}
						else label += "None";
					}
					
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
						model.Transform.Position.Value + (Vector3.up * 3f),
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
					var label = GetId(model);
					
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
						model.Transform.Position.Value + (Vector3.up * 1f),
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
					Handles.DrawWireCube(model.Transform.Position.Value, Vector3.one);
				}
			}

			if (SceneInspectionSettings.IsInspectingLightLevels.Value)
			{
				Handles.color = Color.yellow.NewA(0.05f);
				HandlesExtensions.BeginDepthCheck(CompareFunction.Less);
				{
					var yOffset = 0f;
					foreach (var model in gameState.Payload.Game.GetLightsActive().Where(l => l.Light.IsLightActive()))
					{
						Handles.DrawSolidDisc(
							model.Transform.Position.Value + new Vector3(0f, yOffset, 0f),
							Vector3.up,
							model.Light.LightRange.Value
						);
						yOffset += 0.01f;
					}
				}
				HandlesExtensions.EndDepthCheck();

				var lightSensitiveOffset = Vector3.up * 4f;
				foreach (var model in gameState.Payload.Game.GetLightSensitives())
				{
					Debug.DrawLine(
						model.Transform.Position.Value + lightSensitiveOffset,
						model.Transform.Position.Value + lightSensitiveOffset + (Vector3.up * model.LightSensitive.LightLevel.Value),
						Color.yellow
					);
				}
			}

			if (SceneInspectionSettings.IsInspectingObligations.Value)
			{
				foreach (var model in gameState.Payload.Game.GetObligations())
				{
					var label = GetId(model);

					if (!AppendObligations(ref label, model)) continue;

					var labelColor = "<color=cyan>";
					
					// TODO: Something fancy with how we render the color of this label...
					
					Handles.Label(
						model.Transform.Position.Value + (Vector3.up * 3f),
						StringExtensions.Wrap(
							label,
							labelColor,
							"</color>"
						),
						labelStyle
					);
				}
			}

			if (SceneInspectionSettings.IsInspectingRooms.Value)
			{
				foreach (var model in gameState.Payload.Game.Rooms.AllActive)
				{
					// var label = GetId(model);
					var label = model.Id.Value;

					label += "\nConnections";

					foreach (var kv in model.AdjacentRoomIds.Value)
					{
						// label += "\n  " + Model.ShortenId(kv.Key) + " : ";
						label += "\n  " + kv.Key + " : ";

						if (kv.Value) label += "<color=green>Open";
						else label += "<color=red>Closed";

						label += "</color>";
					}

					var labelColor = "<color=cyan>";
					
					Handles.Label(
						model.Transform.Position.Value + (Vector3.up * 6f),
						StringExtensions.Wrap(
							label,
							labelColor,
							"</color>"
						),
						labelStyle
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
				var valueMaximum = capacity?.GetMaximumFor(type);
				
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