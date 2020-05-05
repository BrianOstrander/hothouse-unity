using System;
using System.Collections.Generic;
using Lunra.Core;
using Lunra.StyxMvp;
using Lunra.StyxMvp.Services;
using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Models.AgentModels;
using UnityEngine;

namespace Lunra.WildVacuum.Services
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

			game.WorldCamera.IsEnabled.Value = true;
			
			var room0 = new RoomPrefabModel();

			room0.PrefabId.Value = "default_spawn";
			room0.IsEnabled.Value = true;
			room0.Id.Value = "room_0";
			
			var room1 = new RoomPrefabModel();

			room1.PrefabId.Value = "rectangle";
			room1.IsEnabled.Value = true;
			room1.Id.Value = "room_1";
			room1.Position.Value = new Vector3(0f, 3.01f, -18.74f);

			game.Rooms.Value = new[]
			{
				room0,
				room1
			};
			
			var door0 = new DoorPrefabModel();

			door0.PrefabId.Value = "default";
			door0.IsEnabled.Value = true;
			door0.RoomConnection.Value = new DoorPrefabModel.Connection(room0.Id.Value, room1.Id.Value);
			door0.Position.Value = new Vector3(0f, -0.02f, -15.74f);

			game.Doors.Value = new[]
			{
				door0
			};
			
			var itemCache0 = new ItemCacheBuildingModel();

			itemCache0.PrefabId.Value = "item_cache";
			itemCache0.IsEnabled.Value = true;
			itemCache0.Position.Value = new Vector3(6f, -0.8386866f, 6f);
			itemCache0.Inventory.Value = Inventory.PopulateMaximum(
				new Dictionary<Item.Types, int>
				{
					{ Item.Types.Stalks, 0 }
				}
			);

			game.ItemCaches.Value = new[]
			{
				itemCache0
			};
			
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
				DwellerModel.Jobs job = DwellerModel.Jobs.Unknown,
				int jobPriority = 0,
				bool debugAgentStates = false
			)
			{
				dweller.Id.Value = id;
				dweller.Position.Value = position;
				dweller.Rotation.Value = Quaternion.identity;
				dweller.NavigationVelocity.Value = 4f;
				dweller.Job.Value = job;
				dweller.JobPriority.Value = jobPriority;
				dweller.IsDebugging = debugAgentStates;
				dweller.NavigationForceDistanceMaximum.Value = 4f;
				dweller.MeleeRange.Value = 0.75f;
				dweller.MeleeCooldown.Value = 0.5f;
				dweller.MeleeDamage.Value = 60f;

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
						{ Item.Types.Stalks, 1 }
					}
				);
			}
			
			game.Dwellers.Activate(
				dweller => initializeDweller(
					dweller,
					"0",
					new Vector3(-12f, -0.8386866f, 6f),
					DwellerModel.Jobs.ClearFlora,
					0,
					true
				)
			);
			
			// game.Dwellers.Activate(
			// 	dweller => initializeDweller(
			// 		dweller,
			// 		"1",
			// 		new Vector3(-10f, -0.8386866f, 6f),
			// 		DwellerModel.Jobs.ClearFlora,
			// 		1
			// 	)
			// );

			/*
			var inventory = Inventory.Populate(
				capacityType => 10,
				entryType => 0
			);
			
			Debug.Log(inventory);

			inventory = inventory.SetCurrent(20, Item.Types.Stalks);

			Debug.Log("SetCurrent 20 Stalks\n" + inventory);
			
			inventory = inventory.SetCurrent(-20, Item.Types.Stalks);

			Debug.Log("SetCurrent -20 Stalks\n" + inventory);
			
			inventory = inventory.Add(15, Item.Types.Stalks);

			Debug.Log("Add 15 Stalks\n" + inventory);
			
			inventory = inventory.Subtract(5, Item.Types.Stalks);

			Debug.Log("Subtract 5 Stalks\n" + inventory);
			
			inventory = inventory.SetMaximum(0, Item.Types.Stalks);

			Debug.Log("SetMaximum 0 Stalks\n" + inventory);
			
			inventory = inventory.Add(item => 5);

			Debug.Log("Add 5 to all\n" + inventory);
			
			Debug.Break();
			*/

			done(Result<GameModel>.Success(game));
		}
	}
}