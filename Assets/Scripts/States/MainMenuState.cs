﻿using System;
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
			game.FloraEffects.IsEnabled.Value = true;
			game.Toolbar.IsEnabled.Value = true;

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
	
			// FLORA
			
			game.Flora.ActivateAdult(
				FloraSpecies.Grass,
				room0.Id.Value,
				new Vector3(7f, 0f, -4f)
			);
			
			game.Flora.ActivateAdult(
				FloraSpecies.Grass,
				room0.Id.Value,
				new Vector3(10f, 0f, -4f)
			);
			
			game.Flora.ActivateAdult(
				FloraSpecies.Grass,
				room0.Id.Value,
				new Vector3(4f, 0f, -4f)
			);
			
			game.Flora.ActivateAdult(
				FloraSpecies.Wheat,
				room0.Id.Value,
				new Vector3(-4f, 0f, -5f)
			);
			
			game.Flora.ActivateAdult(
				FloraSpecies.Wheat,
				room0.Id.Value,
				new Vector3(-6f, 0f, -5f)
			);

			// DEBRIS
			
			game.Debris.Activate(
				room0.Id.Value, 
				new Vector3(0, 0f, -6f)
			);
			
			game.Debris.Activate(
				room0.Id.Value,
				new Vector3(1, 0f, -5f)
			);

			// DWELLERS
			
			var dweller0 = game.Dwellers.Activate(
				room0.Id.Value,
				new Vector3(-4f, -0.8386866f, 3f)
			);
			dweller0.Id.Value = "0";
			dweller0.Job.Value = Jobs.Clearer;
			// dweller0.IsDebugging = true;
			
			var dweller1 = game.Dwellers.Activate(
				room0.Id.Value,
				new Vector3(-4f, -0.8386866f, 3f)
			);
			dweller1.Id.Value = "1";
			dweller1.Job.Value = Jobs.Construction;
			// dweller1.IsDebugging = true;
			
			// BUILDINGS
			
			game.Buildings.Activate(
				Buildings.Bonfire,
				room0.Id.Value,
				new Vector3(2f, -0.8386866f, 6f),
				Quaternion.identity,
				BuildingStates.Operating
			);
			
			/*
			game.Buildings.Activate(
				Buildings.Bonfire,
				room0.Id.Value,
				new Vector3(-6f, -0.8386866f, 6f),
				Quaternion.identity,
				BuildingStates.Constructing
			);

			game.Buildings.Activate(
				Buildings.Bedroll,
				room0.Id.Value,
				new Vector3(-12f, -0.8386866f, 0f),
				Quaternion.identity,
				BuildingStates.Constructing
			);
			*/
			
			var wagon = game.Buildings.Activate(
				Buildings.StartingWagon,
				room0.Id.Value,
				new Vector3(0f, -0.8386866f, 4f),
				Quaternion.identity,
				BuildingStates.Operating
			);

			game.Hints.HintCollections.Value = new[]
			{
				HintCollection.NewDelay(2f),
				HintCollection.New(
					Hint.NewDismissedOnTimeout(
						"Your people are lost, and their fire is low...",
						Condition.Any(Condition.Types.ConstantTrue),
						5f
					)
				),
				HintCollection.NewDelay(1f),
				HintCollection.New(
					Hint.NewDismissedOnCondition(
						"Build another fire to illuminate the darkness...",
						Condition.Any(Condition.Types.ConstantTrue),
						Condition.None(Condition.Types.SingleOperationalFire)
					)
				),
				HintCollection.NewDelay(0.5f),
				HintCollection.New(
					Hint.NewDismissedOnTimeout(
						"All remaining stalks were used to build that fire...",
						Condition.Any(Condition.Types.ConstantTrue),
						4f
					)
				),
				HintCollection.NewDelay(0.5f),
				HintCollection.New(
					Hint.NewDismissedOnCondition(
						"Gather more stalks to keep your fires burning...",
						Condition.Any(Condition.Types.ConstantTrue),
						Condition.None(Condition.Types.NoStalks)
					)
				),
				HintCollection.NewDelay(0.5f),
				HintCollection.New(
					Hint.NewDismissedOnTimeout(
						"Your dwellers won't leave the safety of their campfires for long, build more to explore the area...",
						Condition.Any(Condition.Types.ConstantTrue),
						10f
					)
				),
				HintCollection.NewDelay(0.5f),
				HintCollection.New(
					Hint.NewDismissedOnCondition(
						"Your dwellers grow hungry, mark morsels for gathering...",
						Condition.Any(Condition.Types.NoRations),
						Condition.None(Condition.Types.LowRations)
					)
				),
				HintCollection.NewDelay(0.5f),
				HintCollection.New(
					Hint.NewDismissedOnCondition(
						"Your dwellers need a place to sleep, build a bedroll for them...",
						Condition.Any(Condition.Types.ZeroBeds),
						Condition.None(Condition.Types.ZeroBeds)
					)
				),
				HintCollection.NewDelay(0.5f),
				HintCollection.New(
					Hint.NewDismissedOnTimeout(
						"TO BE CONTINUED",
						Condition.Any(Condition.Types.ConstantTrue),
						10f
					)
				),
			};
			
			done(Result<GameModel>.Success(game));
		}
	}
}