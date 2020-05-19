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
			var game = new GameModel();

			game.SimulationTimeConversion.Value = 1f / 10f;
			
			game.WorldCamera.IsEnabled.Value = true;

			void initializeRoom(
				RoomPrefabModel room,
				string id,
				Vector3 position
			)
			{
				room.Id.Value = id;
				room.RoomId.Value = id;
				room.Position.Value = position;
				room.Rotation.Value = Quaternion.identity;
			}
			
			var room0 = game.Rooms.Activate(
				"default_spawn",
				room => initializeRoom(
					room,
					"room_0",
					Vector3.zero
				)
			);
			
			var room1 = game.Rooms.Activate(
				"rectangle",
				room => initializeRoom(
					room,
					"room_1",
					new Vector3(0f, 3.01f, -18.74f)
				)
			);

			void initializeDoor(
				DoorPrefabModel door,
				DoorPrefabModel.Connection connection,
				Vector3 position
			)
			{
				door.RoomConnection.Value = connection;
				door.Position.Value = position;
			}

			game.Doors.Activate(
				"default",
				door => initializeDoor(
					door,
					new DoorPrefabModel.Connection(room0.Id.Value, room1.Id.Value),
					new Vector3(0f, -0.02f, -15.74f)
				)
			);

			var fastFloraPrefabIds = new string[]
			{
				"fast0",
				"fast1"
			};
			
			var edibleFloraPrefabIds = new string[]
			{
				"edible0"
			};
			
			void initializeFlora(
				FloraModel flora,
				Vector3 position,
				FloraSpecies species
			)
			{
				flora.Species.Value = species;
				flora.RoomId.Value = room0.Id.Value;
				flora.Position.Value = position;
				flora.Rotation.Value = Quaternion.identity;
				flora.Age.Value = Interval.WithMaximum(1f);
				flora.ReproductionRadius.Value = new FloatRange(0.5f, 1f);
				flora.ReproductionFailureLimit.Value = 40;
				flora.HealthMaximum.Value = 100f;
				flora.Health.Value = flora.HealthMaximum.Value;

				switch (species)
				{
					case FloraSpecies.Fast:
						flora.SpreadDamage.Value = 50f;
						flora.ValidPrefabIds.Value = fastFloraPrefabIds;
						flora.ReproductionElapsed.Value = Interval.WithMaximum(2f);
						flora.ItemDrops.Value = new Inventory(
							new Dictionary<Inventory.Types, int>
							{
								{ Inventory.Types.Stalks, 1 }
							}
						);
						break;
					case FloraSpecies.Edible:
						flora.SpreadDamage.Value = 0f;
						flora.ValidPrefabIds.Value = edibleFloraPrefabIds;
						flora.ReproductionElapsed.Value = Interval.WithMaximum(10f);
						flora.ItemDrops.Value = new Inventory(
							new Dictionary<Inventory.Types, int>
							{
								{ Inventory.Types.Rations, 1 }
							}
						);
						break;
					default:
						Debug.LogError("Unrecognized species: "+species);
						break;
				}
			}
			
			game.Flora.Activate(
				fastFloraPrefabIds.First(),
				flora => initializeFlora(
					flora,
					new Vector3(7f, 0f, -5f),
					FloraSpecies.Fast
				)
			);
			
			game.Flora.Activate(
				fastFloraPrefabIds.First(),
				flora => initializeFlora(
					flora,
					new Vector3(10f, 0f, -5f),
					FloraSpecies.Fast
				)
			);
			
			game.Flora.Activate(
				fastFloraPrefabIds.First(),
				flora => initializeFlora(
					flora,
					new Vector3(4f, 0f, -5f),
					FloraSpecies.Fast
				)
			);
			
			game.Flora.Activate(
				edibleFloraPrefabIds.First(),
				flora => initializeFlora(
					flora,
					new Vector3(-4f, 0f, -5f),
					FloraSpecies.Edible
				)
			);
			
			game.Flora.Activate(
				edibleFloraPrefabIds.First(),
				flora => initializeFlora(
					flora,
					new Vector3(-6f, 0f, -5f),
					FloraSpecies.Edible
				)
			);
			
			void initializeClearable(
				ClearableModel clearable,
				Vector3 position,
				Inventory itemDrops
			)
			{
				clearable.RoomId.Value = room0.Id.Value;
				clearable.Position.Value = position;
				clearable.Rotation.Value = Quaternion.identity;
				clearable.ItemDrops.Value = itemDrops;
			}

			game.Debris.Activate(
				"debris_small",
				debris => initializeClearable(
					debris,
					new Vector3(0, 0f, -6f),
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
				debris => initializeClearable(
					debris,
					new Vector3(1, 0f, -5f),
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
				Vector3 position,
				Jobs job = Jobs.None,
				Desires desire = Desires.None,
				bool debugAgentStates = false
			)
			{
				dweller.Id.Value = id;
				dweller.Position.Value = position;
				dweller.Rotation.Value = Quaternion.identity;
				dweller.NavigationVelocity.Value = 4f;
				dweller.Job.Value = job;
				dweller.JobShift.Value = new DayTimeFrame(0.0f, 0.5f);
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
			
			// game.Dwellers.Activate(
			// 	dweller => initializeDweller(
			// 		dweller,
			// 		"0",
			// 		new Vector3(-6f, -0.8386866f, 3f),
			// 		Jobs.Construction
			// 	)
			// );
			//
			// game.Dwellers.Activate(
			// 	dweller => initializeDweller(
			// 		dweller,
			// 		"1",
			// 		new Vector3(-2f, -0.8386866f, 10f),
			// 		Jobs.Construction
			// 	)
			// );
			
			game.Dwellers.Activate(
				dweller => initializeDweller(
					dweller,
					"2",
					new Vector3(-4f, -0.8386866f, 3f),
					Jobs.Clearer,
					debugAgentStates: true
				)
			);
			
			void initializeBuilding(
				BuildingModel model,
				string id,
				Vector3 position,
				Inventory inventory,
				InventoryCapacity inventoryCapacity,
				params DesireQuality[] desireQualities
			)
			{
				model.BuildingState.Value = BuildingStates.Operating;
				model.RoomId.Value = room0.Id.Value;
				model.Id.Value = id;
				model.Position.Value = position;
				model.Inventory.Value = inventory;
				model.InventoryCapacity.Value = inventoryCapacity;
				model.DesireQuality.Value = desireQualities;
			}

			var fireBuilding1 = game.Buildings.Activate(
				"fire_bonfire",
				m =>
				{
					initializeBuilding(
						m,
						"fire_bonfire0",
						new Vector3(-12f, -0.8386866f, 6f),
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
					m.LightFuelInterval.Value = Interval.WithMaximum(10000f);
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
				m => initializeBuilding(
					m,
					"sleep_1",
					new Vector3(-12f, -0.8386866f, 0f),
					Inventory.Empty,
					InventoryCapacity.None(),
					DesireQuality.New(Desires.Sleep, 1f)
				)
			);

			// sleepBuilding1.BuildingState.Value = BuildingStates.Salvaging;
			sleepBuilding1.SalvageInventory.Value = new Inventory(
				new Dictionary<Inventory.Types, int>
				{
					{Inventory.Types.Stalks, 1},
					{Inventory.Types.Scrap, 2}
				}
			);
			/*
			sleepBuilding.ConstructionInventoryCapacity.Value = InventoryCapacity.ByIndividualWeight(
				new Inventory(
					new Dictionary<Item.Types, int>
					{
						{Item.Types.Stalks, 2},
						{Item.Types.Scrap, 2}
					}
				)
			);
			*/
			
			game.Buildings.Activate(
				"default_wagon",
				m => initializeBuilding(
					m,
					"wagon_0",
					new Vector3(0f, -0.8386866f, 4f),
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
				m =>
				{
					initializeBuilding(
						m,
						"fire_bonfire1",
						new Vector3(2f, -0.8386866f, 6f),
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
					m.LightFuelInterval.Value = Interval.WithMaximum(10000f);
					m.IsLightRefueling.Value = true;
				}
			);

			// game.ItemDrops.Activate(
			// 	m =>
			// 	{
			// 		m.Position.Value = new Vector3(1f, 0f, -5f);
			// 		m.Job.Value = Jobs.None;
			// 		m.Inventory.Value = new Inventory(
			// 			new Dictionary<Inventory.Types, int>
			// 			{
			// 				{ Inventory.Types.Scrap, 1 }
			// 			}
			// 		);
			// 	}
			// );
			
			// game.ItemDrops.Activate(
			// 	m =>
			// 	{
			// 		m.Position.Value = new Vector3(0f, 0f, -5f);
			// 		m.Job.Value = Jobs.ClearFlora;
			// 		m.Inventory.Value = new Inventory(
			// 			new Dictionary<Item.Types, int>
			// 			{
			// 				{ Item.Types.Rations, 1 }
			// 			}
			// 		);
			// 	}
			// );

			done(Result<GameModel>.Success(game));
		}
	}
}