using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.Editor.Core;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Services;
using Lunra.Hothouse.Services.Editor;
using Lunra.Hothouse.Views;
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

		const float SceneCameraRadius = 10f;
		
		static GUIStyle labelStyle;
		
		static List<string> obligationIdsHandled = new List<string>();
		static GameState gameState;
		static Vector3 sceneCameraPosition;
		
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

			if (!GameStateEditorUtility.GetGameState(out gameState)) return;

			obligationIdsHandled.Clear();

			var roomRevealDistances = gameState.Payload.Game.Rooms.AllActive.ToDictionary(
				m => m.RoomTransform.Id.Value,
				m => m.RevealDistance.Value
			);

			sceneCameraPosition = SceneView.currentDrawingSceneView.camera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, SceneCameraRadius));

			bool isInInspectedRoom(IRoomTransformModel model)
			{
				if (roomRevealDistances[model.RoomTransform.Id.Value] == 0) return true;
				if (model is DoorModel doorModel)
				{
					return roomRevealDistances[doorModel.RoomConnection.Value.RoomId0] == 0 || roomRevealDistances[doorModel.RoomConnection.Value.RoomId1] == 0;
				}
				return false;
			}
			
			if (SceneInspectionSettings.IsInspectingLightLevels.Value)
			{
				Handles.color = Color.yellow.NewA(0.05f);
				HandlesExtensions.BeginDepthCheck(CompareFunction.Less);
				{
					var yOffset = 0.01f;
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
				foreach (var model in gameState.Payload.Game.GetLightSensitives().Where(isInInspectedRoom))
				{
					Debug.DrawLine(
						model.Transform.Position.Value + lightSensitiveOffset,
						model.Transform.Position.Value + lightSensitiveOffset + (Vector3.up * model.LightSensitive.LightLevel.Value),
						Color.yellow
					);
				}
			}

			if (SceneInspectionSettings.IsInspectingRooms.Value)
			{
				foreach (var model in gameState.Payload.Game.Rooms.AllActive.Where(isInInspectedRoom))
				{
					DrawLabel(
						model,
						append =>
						{
							if (model.IsSpawn.Value) append("Is Spawn: true");
							else
							{
								if (0 < model.SpawnDistance.Value) append("Spawn Distance: " + model.SpawnDistance.Value);
								if (0 < model.RevealDistance.Value) append("Reveal Distance: " + model.RevealDistance.Value);
							}

							var connectionsResult = "Connections";

							foreach (var kv in model.AdjacentRoomIds.Value)
							{
								connectionsResult += "\n  " + Model.ShortenId(kv.Key) + " : ";

								if (kv.Value) connectionsResult += "<color=green>Open";
								else connectionsResult += "<color=red>Closed";

								connectionsResult += "</color>";
							}

							append(connectionsResult);
						},
						ignoreRenderLimiting: true
					);
				}
			}
			
			if (SceneInspectionSettings.IsInspectingBuildings.Value)
			{
				foreach (var model in gameState.Payload.Game.Buildings.AllActive.Where(isInInspectedRoom))
				{
					DrawEntranceInspection(model);
					
					DrawLabel(
						model,
						append =>
						{
							if (model.BuildingState.Value != BuildingStates.Operating)
							{
								append("State: " + model.BuildingState.Value);
							}
					
							switch (model.BuildingState.Value)
							{
								case BuildingStates.Constructing:
									append("Construction " + model.ConstructionInventory);
									break;
								case BuildingStates.Salvaging:
									append("Salvage " + model.SalvageInventory);
									break;
							}
						}
					);

					if (model.BuildingState.Value == BuildingStates.Constructing)
					{
						Handles.color = Color.yellow.NewA(0.2f);
						Handles.DrawWireCube(model.Transform.Position.Value, Vector3.one);
					}
				}
			}

			if (SceneInspectionSettings.IsInspectingDwellers.Value)
			{
				foreach (var model in gameState.Payload.Game.Dwellers.AllActive.Where(isInInspectedRoom))
				{
					DrawLabel(
						model,
						append =>
						{
							if (model.Job.Value != Jobs.None) append("Job: " + model.Job.Value);
							if (!model.Workplace.Value.IsNull) append("Workplace: " + model.Workplace.Value);
						},
						model.Name.Value
					);
				}
			}
			
			if (SceneInspectionSettings.IsInspectingOtherAgents.Value)
			{
				foreach (var model in gameState.Payload.Game.Seekers.AllActive.Where(isInInspectedRoom))
				{
					DrawLabel(
						model
					);
				}
			}
			
			if (SceneInspectionSettings.IsInspectingItemDrops.Value)
			{
				foreach (var model in gameState.Payload.Game.ItemDrops.AllActive.Where(isInInspectedRoom))
				{
					DrawLabel(
						model
					);
				}
			}
			
			if (SceneInspectionSettings.IsInspectingGenerators.Value)
			{
				foreach (var model in gameState.Payload.Game.Generators.AllActive.Where(isInInspectedRoom))
				{
					DrawLabel(
						model
					);
				}
			}

			foreach (var model in gameState.Payload.Game.Flora.AllActive.Where(isInInspectedRoom))
			{
				if (!SceneInspectionSettings.IsInspectingFlora.Value)
				{
					if (!(SceneInspectionSettings.IsInspectingObligations.Value && model.Obligations.HasAny())) continue;
				}
				
				DrawEntranceInspection(model);

				DrawLabel(
					model,
					append =>
					{
						if (model.Age.Value.IsDone)
						{
							append($"Reproduction: \t{model.ReproductionElapsed.Value.Normalized:N2}");
							append($"Reproduction {model.ReproductionModifier}");
						}
						else
						{
							append($"Age: \t{model.Age.Value.Normalized:N2}");
							append($"Age {model.AgeModifier}");
						}
					}
				);
					
				if (model.IsReproducing.Value) continue;
				Handles.color = Color.red;
				Handles.DrawWireCube(model.Transform.Position.Value, Vector3.one);
			}
			
			foreach (var model in gameState.Payload.Game.Doors.AllActive.Where(isInInspectedRoom))
			{
				if (!SceneInspectionSettings.IsInspectingDoors.Value)
				{
					if (!(SceneInspectionSettings.IsInspectingObligations.Value && model.Obligations.HasAny())) continue;
				}
				
				DrawEntranceInspection(model);
				
				DrawLabel(
					model,
					append =>
					{
						append("RoomId0: " + Model.ShortenId(model.RoomConnection.Value.RoomId0));
						append("RoomId1: " + Model.ShortenId(model.RoomConnection.Value.RoomId1));
						append("ConnectedRoomId: " + Model.ShortenId(model.LightSensitive.ConnectedRoomId.Value));		
					},
					ignoreRenderLimiting: true
				);
					
				DrawSelectionButton(
					model,
					new GUIContent("Select")
				);
			}
		}
		
		public static void OpenHandlerAsset()
		{
			AssetDatabase.OpenAsset(
				AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/Scripts/Editor/" + nameof(SceneInspectionHandler) + ".cs"),
				40
			);
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

		static void DrawLabel(
			IModel model,
			Action<Action<string>> append = null,
			string overrideName = null,
			bool ignoreRenderLimiting = false
		)
		{
			var position = Vector3.zero;

			if (model is ITransformModel transformModel) position = transformModel.Transform.Position.Value;
			else Debug.LogError("Trying to draw a label on a non-transform model");

			if (!ignoreRenderLimiting && SceneCameraRadius < Vector3.Distance(position, sceneCameraPosition)) return;
			
			var result = "Id: " + model.ShortId;

			if (!string.IsNullOrEmpty(overrideName)) result += " - " + overrideName;

			void appendResult(string addition)
			{
				result += "\n" + addition;
			}

			if (SceneInspectionSettings.IsInspectingRooms.Value && model is IRoomTransformModel roomTransformModel)
			{
				appendResult(roomTransformModel.RoomTransform.ToString());
			}

			if (model is AgentModel agentModel)
			{
				appendResult("Ai State: " + agentModel.Context.CurrentState);
				
				if (DrawButton(agentModel, new GUIContent("Serialize State")))
				{
					EditorGUIUtility.systemCopyBuffer = agentModel.StateMachine.GetSerializedGraph(true);
					Debug.Log("Serialized ai state copied to clipboard");
				}
			}
			
			if (model is IHealthModel healthModel && healthModel.Health.IsDamaged)
			{
				appendResult(healthModel.Health.ToString());
			}

			if (SceneInspectionSettings.IsInspectingObligations.Value)
			{
				if (model is IObligationModel obligationModel && obligationModel.Obligations.HasAny())
				{
					appendResult(obligationModel.Obligations.ToString());
				}

				if (model is IObligationPromiseModel obligationPromiseModel && obligationPromiseModel.ObligationPromises.HasAny())
				{
					appendResult(obligationPromiseModel.ObligationPromises.ToString());
				}
			}
			
			if (model is IClaimOwnershipModel claimOwnershipModel && 0 < claimOwnershipModel.Ownership.MaximumClaimers.Value)
			{
				appendResult(claimOwnershipModel.Ownership.ToString());
			}

			if (SceneInspectionSettings.IsInspectingLightLevels.Value)
			{
				if (model is ILightModel lightModel && lightModel.Light.IsLight.Value)
				{
					appendResult(lightModel.Light.ToString());
				}

				if (model is ILightSensitiveModel lightSensitiveModel)
				{
					appendResult(lightSensitiveModel.LightSensitive.ToString());
				}
			}

			if (model is IInventoryModel inventoryModel)
			{
				appendResult("Inventory " + inventoryModel.Inventory);
			}

			if (model is IInventoryPromiseModel inventoryPromiseModel)
			{
				appendResult(inventoryPromiseModel.InventoryPromises.ToString());
			}

			if (model is IRecipeModel recipeModel && (recipeModel.Recipes.Available.Value.Any() || recipeModel.Recipes.Queue.Value.Any()))
			{
				appendResult(recipeModel.Recipes.ToString());
			}

			if (model is IGeneratorModel generatorModel)
			{
				appendResult(generatorModel.Generator.ToString(gameState.Payload.Game.SimulationTime.Value));
			}
			
			if (model is IFarmModel farmModel && farmModel.Farm.IsFarm)
			{
				appendResult(farmModel.Farm.ToString());

				Debug.DrawLine(farmModel.Transform.Position.Value, farmModel.Transform.Position.Value + Vector3.up, Color.red);
				
				var farmRight = (farmModel.Transform.Rotation.Value * Vector3.right) * (farmModel.Farm.Size.x * 0.5f);
				var farmForward = (farmModel.Transform.Rotation.Value * Vector3.forward) * (farmModel.Farm.Size.y * 0.5f);

				var farmBoundaries = new[]
				{
					farmForward + farmRight,
					farmRight - farmForward,
					(-farmForward) - farmRight,
					farmForward - farmRight
				};
					
				Handles.color = Color.green;
				
				Handles.matrix = Matrix4x4.TRS(
					farmModel.Transform.Position.Value,
					Quaternion.identity, 
					Vector3.one
				);
					
				Handles.DrawDottedLines(
					farmBoundaries,
					new []
					{
						0, 1,
						1, 2,
						2, 3,
						3, 0
					},
					4f
				);
				
				Handles.matrix = Matrix4x4.identity;
					
				foreach (var farmPlot in farmModel.Farm.Plots)
				{
					var farmPlotColor = Color.magenta;

					switch (farmPlot.State)
					{
						case FarmPlot.States.Blocked:
						case FarmPlot.States.Invalid:
							farmPlotColor = Color.red;
							break;
						case FarmPlot.States.ReadyToSow:
							farmPlotColor = Color.green;
							break;
						case FarmPlot.States.Sown:
							farmPlotColor = Color.yellow;
							break;
					}

					Handles.color = farmPlot.AttendingFarmer.IsNull ? farmPlotColor.NewA(0.5f) : farmPlotColor;

					Handles.DrawWireDisc(
						farmPlot.Position,
						Vector3.up,
						0.2f
					);
				}
			}

			if (model is IGoalModel goalModel)
			{
				appendResult(goalModel.Goals.ToString());
			}

			if (model is IGoalActivityModel goalActivityModel)
			{
				appendResult(goalActivityModel.Activities.ToString());
			}

			if (model is ITagModel tagModel)
			{
				appendResult(tagModel.Tags.ToString());
			}

			append?.Invoke(appendResult);

			Handles.Label(
				position + (Vector3.up * 3f),
				StringExtensions.Wrap(result, "<color=cyan>", "</color>"),
				labelStyle
			);
		}

		static void DrawEntranceInspection(IEnterableModel model)
		{	
			if (!SceneInspectionSettings.IsInspectingEntrances.Value) return;
			
			HandlesExtensions.BeginDepthCheck(CompareFunction.Less);
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
			HandlesExtensions.EndDepthCheck();
		}

		static void DrawSelectionButton(
			ITransformModel model,
			GUIContent content,
			Vector3? offset = null
		)
		{
			if (DrawButton(model, content, offset))
			{
				var view = GameObject.FindObjectsOfType<PrefabView>()
					.FirstOrDefault(v => v.ModelId == model.Id.Value);

				if (view == null)
				{
					EditorWindow.focusedWindow.ShowNotification(new GUIContent("No view found with id: "+model.ShortId));
				}
				else
				{
					Selection.activeGameObject = view.gameObject;
				}
			}
		}

		static bool DrawButton(
			ITransformModel model,
			GUIContent content,
			Vector3? offset = null
		)
		{
			var result = false;
			offset = offset ?? Vector3.zero;

			var guiPoint = HandleUtility.WorldToGUIPointWithDepth(model.Transform.Position.Value + offset.Value);

			if (guiPoint.z < 0f) return result;
			
			Handles.BeginGUI();
			{
				var rect = new Rect(
					guiPoint,
					EditorStyles.miniButton.CalcSize(content)
				);

				result = GUI.Button(rect, content, EditorStyles.miniButton);
			}
			Handles.EndGUI();

			return result;
		}
	}
}