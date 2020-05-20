using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Models.AgentModels;
using Lunra.StyxMvp;
using Lunra.StyxMvp.Services;
using UnityEngine;

namespace Lunra.Hothouse.Services
{
	public class MainMenuPayload : IStatePayload
	{
		public PreferencesModel Preferences;
	}

	public class MainMenuState : State<MainMenuPayload>
	{
		// Reminder: Keep variables in payload for easy reset of states!

		static string[] Scenes => new []
		{
			"MainMenu"
		};
		
		#region Begin
		protected override void Begin()
		{
			App.S.PushBlocking(
				done => App.Scenes.Request(SceneRequest.Load(result => done(), Scenes))	
			);
		}
		#endregion

		#region Idle
		protected override void Idle()
		{
			GenerateNewGame(
				result =>
				{
					if (result.Status != ResultStatus.Success)
					{
						result.Log("Generating new game did not succeed!");
						return;
					}
					
					App.S.RequestState(
						new GamePayload
						{
							Preferences = Payload.Preferences,
							Game = result.Payload
						}
					);
				}
			);
			
		}
		#endregion
		
		#region End
		protected override void End()
		{
			App.S.PushBlocking(
				done => App.P.UnRegisterAll(done)
			);

			App.S.PushBlocking(
				done => App.Scenes.Request(SceneRequest.UnLoad(result => done(), Scenes))
			);
		}
		#endregion

		void GenerateNewGame(Action<Result<GameModel>> done)
		{
			var game = App.M.Create<GameModel>(App.M.CreateUniqueId());

			game.SimulationTimeConversion.Value = 1f / 10f;
			
			game.WorldCamera.IsEnabled.Value = true;

			void initializeRoom(RoomPrefabModel room)
			{
				room.Id.Value = room.RoomId.Value;
			}
			
			var room0 = game.Rooms.Activate(
				"default_spawn",
				"room_0",
				Vector3.zero,
				Quaternion.identity,
				initializeRoom
			);
			
			var room1 = game.Rooms.Activate(
				"rectangle",
				"room_1",
				new Vector3(0f, 3.01f, -18.74f),
				Quaternion.identity,
				initializeRoom
			);

			void initializeDoor(
				DoorPrefabModel door,
				DoorPrefabModel.Connection connection
			)
			{
				door.RoomConnection.Value = connection;
			}

			game.Doors.Activate(
				"default",
				room0.Id.Value,
				new Vector3(0f, -0.02f, -15.74f),
				Quaternion.identity,
				door => initializeDoor(
					door,
					new DoorPrefabModel.Connection(room0.Id.Value, room1.Id.Value)
				)
			);
	
			game.Flora.ActivateAdult(
				FloraSpecies.Fast,
				room0.Id.Value,
				new Vector3(7f, 0f, -4f)
			);
			
			game.Flora.ActivateAdult(
				FloraSpecies.Fast,
				room0.Id.Value,
				new Vector3(10f, 0f, -4f)
			);
			
			game.Flora.ActivateAdult(
				FloraSpecies.Fast,
				room0.Id.Value,
				new Vector3(4f, 0f, -4f)
			);
			
			game.Flora.ActivateAdult(
				FloraSpecies.Edible,
				room0.Id.Value,
				new Vector3(-4f, 0f, -5f)
			);
			
			game.Flora.ActivateAdult(
				FloraSpecies.Edible,
				room0.Id.Value,
				new Vector3(-6f, 0f, -5f)
			);

			void initializeClearable(
				ClearableModel clearable,
				Inventory itemDrops
			)
			{
				clearable.ItemDrops.Value = itemDrops;
			}

			game.Debris.Activate(
				"debris_small",
				room0.Id.Value, 
				new Vector3(0, 0f, -6f),
				Quaternion.identity,
				debris => initializeClearable(
					debris,
					new Inventory(
						new Dictionary<Inventory.Types, int>
						{
							{ Inventory.Types.Scrap, 1 }
						}
					)
				)
			);
			
			game.Debris.Activate(
				"debris_large",
				room0.Id.Value,
				new Vector3(1, 0f, -5f),
				Quaternion.identity,
				debris => initializeClearable(
					debris,
					new Inventory(
						new Dictionary<Inventory.Types, int>
						{
							{ Inventory.Types.Scrap, 1 }
						}
					)
				)
			);

			void initializeDweller(
				DwellerModel dweller,
				string id,
				Jobs job = Jobs.None,
				Desires desire = Desires.None,
				bool debugAgentStates = false
			)
			{
				dweller.Id.Value = id;
				dweller.NavigationVelocity.Value = 4f;
				dweller.Job.Value = job;
				dweller.JobShift.Value = new DayTimeFrame(0.0f, 1f);
				dweller.Desire.Value = desire;
				dweller.IsDebugging = debugAgentStates;
				dweller.NavigationForceDistanceMaximum.Value = 4f;
				dweller.MeleeRange.Value = 0.75f;
				dweller.MeleeCooldown.Value = 0.5f;
				dweller.MeleeDamage.Value = 60f;
				dweller.HealthMaximum.Value = 100f;
				dweller.Health.Value = dweller.HealthMaximum.Value;

				dweller.WithdrawalCooldown.Value = 0.5f;
				dweller.DepositCooldown.Value = dweller.WithdrawalCooldown.Value;
				dweller.InventoryCapacity.Value = InventoryCapacity.ByTotalWeight(2);
				
				dweller.DesireDamage.Value = new Dictionary<Desires, float>
				{
					{ Desires.Eat , 0.3f },
					{ Desires.Sleep , 0.1f }
				};
			}

			game.Dwellers.Activate(
				"default",
				room0.Id.Value,
				new Vector3(-4f, -0.8386866f, 3f),
				Quaternion.identity,
				dweller => initializeDweller(
					dweller,
					"2",
					Jobs.Clearer,
					debugAgentStates: true
				)
			);
			
			void initializeBuilding(
				BuildingModel model,
				string id,
				Inventory inventory,
				InventoryCapacity inventoryCapacity,
				params DesireQuality[] desireQualities
			)
			{
				model.BuildingState.Value = BuildingStates.Operating;
				model.Id.Value = id;
				model.Inventory.Value = inventory;
				model.InventoryCapacity.Value = inventoryCapacity;
				model.DesireQuality.Value = desireQualities;
			}

			var fireBuilding1 = game.Buildings.Activate(
				"fire_bonfire",
				room0.Id.Value,
				new Vector3(-6f, -0.8386866f, 6f),
				Quaternion.identity,
				m =>
				{
					initializeBuilding(
						m,
						"fire_bonfire0",
						Inventory.Empty, 
						InventoryCapacity.ByIndividualWeight(
							new Inventory(
								new Dictionary<Inventory.Types, int>
								{
									{ Inventory.Types.Stalks, 50 }
								}
							)
						)
					);

					m.LightState.Value = LightStates.Fueled;
					m.LightFuel.Value = new Inventory(
						new Dictionary<Inventory.Types, int>
						{
							{ Inventory.Types.Stalks, 1 }
						}
					);
					
					m.SalvageInventory.Value = new Inventory(
						new Dictionary<Inventory.Types, int>
						{
							{ Inventory.Types.Stalks, 1 }
						}
					);
					
					m.LightFuelInterval.Value = Interval.WithMaximum(1f);
					m.IsLightRefueling.Value = true;
				}
			);

			fireBuilding1.BuildingState.Value = BuildingStates.Constructing;
			fireBuilding1.ConstructionInventoryCapacity.Value = InventoryCapacity.ByIndividualWeight(
				new Inventory(
					new Dictionary<Inventory.Types, int>
					{
						{Inventory.Types.Stalks, 1},
						// {Inventory.Types.Scrap, 1}
					}
				)
			);
			
			var sleepBuilding1 = game.Buildings.Activate(
				"debug",
				room0.Id.Value,
				new Vector3(-12f, -0.8386866f, 0f),
				Quaternion.identity,
				m => initializeBuilding(
					m,
					"sleep_1",
					Inventory.Empty,
					InventoryCapacity.None(),
					DesireQuality.New(Desires.Sleep, 1f)
				)
			);
			
			sleepBuilding1.BuildingState.Value = BuildingStates.Salvaging;
			sleepBuilding1.SalvageInventory.Value = new Inventory(
				new Dictionary<Inventory.Types, int>
				{
					{Inventory.Types.Stalks, 1},
					{Inventory.Types.Scrap, 2}
				}
			);
			
			game.Buildings.Activate(
				"default_wagon",
				room0.Id.Value,
				new Vector3(0f, -0.8386866f, 4f),
				Quaternion.identity,
				m => initializeBuilding(
					m,
					"wagon_0",
					new Inventory(
						new Dictionary<Inventory.Types, int>
						{
							// { Item.Types.Stalks, 4 },
							// { Item.Types.Scrap, 4 },
							// { Item.Types.Rations, 4 }
						}
					),
					InventoryCapacity.ByIndividualWeight(
						new Inventory(
							new Dictionary<Inventory.Types, int>
							{
								// { Inventory.Types.Stalks, 50 },
								// { Inventory.Types.Scrap, 50 },
								// { Inventory.Types.Rations, 50 }
							}
						)	
					),
					new DesireQuality(
						Desires.Eat, 
						new Inventory(
							new Dictionary<Inventory.Types, int>
							{
								{ Inventory.Types.Rations, 1 }
							}
						),
						1f
					)
				)
			);
			
			game.Buildings.Activate(
				"fire_bonfire",
				room0.Id.Value,
				new Vector3(2f, -0.8386866f, 6f),
				Quaternion.identity,
				m =>
				{
					initializeBuilding(
						m,
						"fire_bonfire1",
						Inventory.Empty, 
						InventoryCapacity.ByIndividualWeight(
							new Inventory(
								new Dictionary<Inventory.Types, int>
								{
									{ Inventory.Types.Stalks, 2 }
								}
							)
						)
					);

					m.LightState.Value = LightStates.Fueled;
					m.LightFuel.Value = new Inventory(
						new Dictionary<Inventory.Types, int>
						{
							{ Inventory.Types.Stalks, 1 }
						}
					);
					m.LightFuelInterval.Value = Interval.WithMaximum(10f);
					m.IsLightRefueling.Value = true;
				}
			);
			

			done(Result<GameModel>.Success(game));
		}
	}
}