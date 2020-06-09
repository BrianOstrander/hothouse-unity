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

			game.DesireDamageMultiplier.Value = 1f;
			game.SimulationTimeConversion.Value = 1f / 10f;
			
			game.FloraEffects.IsEnabled.Value = true;
			game.Toolbar.IsEnabled.Value = true;
			
			game.WorldCamera.IsEnabled.Value = true;
			game.WorldCamera.PanVelocity.Value = 12f;
			game.WorldCamera.OrbitVelocity.Value = 64f;
			
			game.WorldCamera.Transform.Position.Value = new Vector3(
				2.5f,
				0f,
				11f
			);

			game.WorldCamera.Transform.Rotation.Value = Quaternion.LookRotation(
				new Vector3(
					-1f,
					0f,
					-1f
				).normalized,
				Vector3.up
			);

			void initializeRoom(RoomPrefabModel room)
			{
				room.Id.Value = room.RoomTransform.Id.Value;
			}
			
			var room0 = game.Rooms.Activate(
				"default_spawn",
				"room_0",
				Vector3.zero,
				Quaternion.identity,
				initializeRoom
			);

			room0.IsExplored.Value = true;
			
			var room1 = game.Rooms.Activate(
				"default_spawn",
				"room_1",
				new Vector3(0f, 0f, -15.74f * 2f),
				Quaternion.AngleAxis(180f, Vector3.up),
				initializeRoom
			);

			game.Doors.Activate(
				room0.Id.Value,
				room1.Id.Value,
				new Vector3(0f, -0.02f, -15.74f),
				Quaternion.identity
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

			game.Flora.ActivateAdult(
				FloraSpecies.Shroom,
				room1.Id.Value,
				new Vector3(0f, -0.02f, -20.74f)
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
			
			game.Buildings.Activate(
				Buildings.StartingWagon,
				room0.Id.Value,
				new Vector3(0f, -0.8386866f, 4f),
				Quaternion.identity,
				BuildingStates.Operating
			);
			
			// HINTS			

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
						"You need to find a source of food for your dwellers, keep exploring to find edible flora...",
						Condition.Any(Condition.Types.ConstantTrue),
						Condition.All(Condition.Types.SeenEdibleFlora)
					)
				),
				HintCollection.NewDelay(0.5f),
				HintCollection.New(
					Hint.NewDismissedOnCondition(
						"Finally, something edible, mark them for gathering...",
						Condition.Any(Condition.Types.ConstantTrue),
						Condition.None(Condition.Types.NoRations)
					)
				),
				HintCollection.NewDelay(0.5f),
				HintCollection.New(
					Hint.NewDismissedOnCondition(
						"Your dwellers grow weary, build a bedroll so they have a place to rest...",
						Condition.Any(Condition.Types.ConstantTrue),
						Condition.None(Condition.Types.ZeroBeds)
					)
				),
				HintCollection.NewDelay(0.5f),
				HintCollection.New(
					Hint.NewDismissedOnCondition(
						"Keep exploring to find doorways to new areas...",
						Condition.Any(Condition.Types.ConstantTrue),
						Condition.Any(Condition.Types.AnyDoorsOpen, Condition.Types.AnyDoorsClosedAndLit)
					)
				),
				HintCollection.NewDelay(0.5f),
				HintCollection.New(
					Hint.NewDismissedOnCondition(
						"Click on doors to instruct your dwellers to open them...",
						Condition.Any(Condition.Types.ConstantTrue),
						Condition.None(Condition.Types.ZeroDoorsOpen)
					)
				),
				HintCollection.NewDelay(0.5f),
				HintCollection.New(
					Hint.NewDismissedOnTimeout(
						"New areas lead to unknown dangers, exercise caution when exploring...",
						Condition.Any(Condition.Types.ConstantTrue),
						8f
					)
				),
				HintCollection.NewDelay(0.5f),
				HintCollection.New(
					Hint.NewDismissedOnTimeout(
						"Watch out for Fast Wart, it can grow out of control...",
						Condition.All(Condition.Types.SeenAttackFlora),
						8f
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
			
			// DEBUGGING
			
			game.DesireDamageMultiplier.Value = 0f;
			
			game.Flora.ActivateAdult(
				FloraSpecies.Grass,
				room0.Id.Value,
				new Vector3(-2f, -0.8386866f, 6f)
			);
			
			// var wagon = game.Buildings.AllActive.First(b => b.Type.Value == Buildings.StartingWagon);
			//
			// wagon.Inventory.Value += (Inventory.Types.Rations, 50);
			
			// game.Buildings.Activate(
			// 	Buildings.Bonfire,
			// 	room0.Id.Value,
			// 	new Vector3(-2.68479f, 0.000807253062f, -5.512804f),
			// 	Quaternion.identity,
			// 	BuildingStates.Operating
			// );

			// var door0 = game.Doors.AllActive.First();
			//
			// game.ObligationIndicators.Register(
			// 	Obligation.New(
			// 		ObligationCategories.Door.Open,
			// 		0,
			// 		ObligationCategories.GetJobs(Jobs.Construction),
			// 		Obligation.ConcentrationRequirements.Instant,
			// 		Interval.Zero()
			// 	),
			// 	door0
			// );

			done(Result<GameModel>.Success(game));
		}
	}
}