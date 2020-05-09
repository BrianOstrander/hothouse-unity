using System;
using System.Collections.Generic;
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

			void initializeFlora(
				FloraModel flora,
				Vector3 position
			)
			{
				flora.RoomId.Value = room0.Id.Value;
				flora.Position.Value = position;
				flora.Rotation.Value = Quaternion.identity;
				flora.Age.Value = Interval.WithMaximum(1f);
				flora.ReproductionElapsed.Value = Interval.WithMaximum(2f);
				flora.ReproductionRadius.Value = new FloatRange(0.5f, 1f);
				flora.ReproductionFailureLimit.Value = 40;
				// flora.ReproductionFailureLimit.Value = 0;
				flora.HealthMaximum.Value = 100f;
				flora.Health.Value = flora.HealthMaximum.Value;
				flora.ItemDrops.Value = Inventory.Populate(
					new Dictionary<Item.Types, int>
					{
						{ Item.Types.Stalks, 1 }
					}
				);
			}
			
			game.Flora.Activate(
				flora => initializeFlora(flora, new Vector3(7f, 0f, -5f))
			);
			
			game.Flora.Activate(
				flora => initializeFlora(flora, new Vector3(10f, 0f, -5f))
			);
			
			game.Flora.Activate(
				flora => initializeFlora(flora, new Vector3(4f, 0f, -5f))
			);
			
			// game.Flora.Activate(
			// 	flora => initializeFlora(flora, new Vector3(-12f, -0.8386866f, 6f))
			// );
			
			void initializeDweller(
				DwellerModel dweller,
				string id,
				Vector3 position,
				Jobs job = Jobs.None,
				int jobPriority = 0,
				Desires desire = Desires.None,
				bool debugAgentStates = false
			)
			{
				dweller.Id.Value = id;
				dweller.Position.Value = position;
				dweller.Rotation.Value = Quaternion.identity;
				dweller.NavigationVelocity.Value = 4f;
				dweller.Job.Value = job;
				dweller.JobPriority.Value = jobPriority;
				dweller.JobShift.Value = new DayTimeFrame(0.0f, 0.1f);
				dweller.Desire.Value = desire;
				dweller.IsDebugging = debugAgentStates;
				dweller.NavigationForceDistanceMaximum.Value = 4f;
				dweller.MeleeRange.Value = 0.75f;
				dweller.MeleeCooldown.Value = 0.5f;
				dweller.MeleeDamage.Value = 60f;
				dweller.HealthMaximum.Value = 100f;
				dweller.Health.Value = dweller.HealthMaximum.Value;

				dweller.LoadCooldown.Value = 0.5f;
				dweller.UnloadCooldown.Value = dweller.LoadCooldown.Value;
				// dweller.Inventory.Value = Inventory.Populate(
				// 	new Dictionary<Item.Types, int>
				// 	{
				// 		{ Item.Types.Stalks, 5 }
				// 	}
				// );
				
				dweller.Inventory.Value = Inventory.PopulateMaximum(
					new Dictionary<Item.Types, int>
					{
						{ Item.Types.Stalks, 2 }
					}
				);
				
				dweller.DesireDamage.Value = new Dictionary<Desires, float>
				{
					{ Desires.Eat , 0.3f },
					{ Desires.Sleep , 0.1f }
				};
			}
			
			game.Dwellers.Activate(
				dweller => initializeDweller(
					dweller,
					"0",
					new Vector3(-12f, -0.8386866f, 3f),
					Jobs.ClearFlora,
					0
				)
			);

			void initializeBuilding(
				BuildingModel model,
				string id,
				Vector3 position,
				Inventory inventory,
				params DesireQuality[] desireQualities
			)
			{
				model.Id.Value = id;
				model.Position.Value = position;
				model.Inventory.Value = inventory;
				model.DesireQuality.Value = desireQualities;
			}

			/*
			game.Buildings.Activate(
				"debug",
				m => initializeBuilding(
					m,
					"sleep_0",
					new Vector3(-12f, -0.8386866f, 6f),
					Inventory.Empty,
					DesireQuality.New(Desires.Sleep, 1f)
				)
			);
			*/

			game.Buildings.Activate(
				"debug",
				m => initializeBuilding(
					m,
					"item_cache_0",
					new Vector3(6f, -0.8386866f, 6f),
					Inventory.PopulateMaximum(
						new Dictionary<Item.Types, int>
						{
							{ Item.Types.Stalks, 999 }
						}
					) 
				)
			);
			
			/*
			game.Buildings.Activate(
				"debug",
				m => initializeBuilding(
					m,
					"eat_0",
					new Vector3(12f, -0.8386866f, 2f),
					Inventory.Populate(
						new Dictionary<Item.Types, int>
						{
							{ Item.Types.Rations, 99 }
						}
					),
					new DesireQuality(
						Desires.Eat, 
						Inventory.Populate(
							new Dictionary<Item.Types, int>
							{
								{ Item.Types.Rations, 1 }
							}
						),
						1f
					)
				)
			);
			*/
			
			game.Buildings.Activate(
				"default_wagon",
				m => initializeBuilding(
					m,
					"wagon_0",
					new Vector3(0f, -0.8386866f, 4f),
					Inventory.Populate(
						new Dictionary<Item.Types, int>
						{
							{ Item.Types.Stalks, 10 },
							{ Item.Types.Rations, 10 }
						}
					),
					new DesireQuality(
						Desires.Eat, 
						Inventory.Populate(
							new Dictionary<Item.Types, int>
							{
								{ Item.Types.Rations, 1 }
							}
						),
						1f
					)
				)
			);

			game.ItemDrops.Activate(
				m =>
				{
					m.Position.Value = new Vector3(2f, 0f, -5f);
					m.Job.Value = Jobs.ClearFlora;
					m.Inventory.Value = Inventory.Populate(
						new Dictionary<Item.Types, int>
						{
							{ Item.Types.Stalks, 1 }
						}
					);
				}
			);
			
			game.ItemDrops.Activate(
				m =>
				{
					m.Position.Value = new Vector3(1f, 0f, -5f);
					m.Job.Value = Jobs.ClearFlora;
					m.Inventory.Value = Inventory.Populate(
						new Dictionary<Item.Types, int>
						{
							{ Item.Types.Stalks, 1 }
						}
					);
				}
			);
			
			game.ItemDrops.Activate(
				m =>
				{
					m.Position.Value = new Vector3(0f, 0f, -5f);
					m.Job.Value = Jobs.ClearFlora;
					m.Inventory.Value = Inventory.Populate(
						new Dictionary<Item.Types, int>
						{
							{ Item.Types.Stalks, 1 }
						}
					);
				}
			);

			done(Result<GameModel>.Success(game));
		}
	}
}