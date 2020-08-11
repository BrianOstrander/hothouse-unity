using System;
using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.NumberDemon;
using Lunra.StyxMvp;
using Lunra.StyxMvp.Services;
using UnityEngine;

namespace Lunra.Hothouse.Services
{
	public class MainMenuPayload : IStatePayload
	{
		public MainMenuModel MainMenu = new MainMenuModel();
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
			Payload.MainMenu.StartGame += OnMainMenuStartGame;
			Payload.MainMenu.CreateGame = OnCreateGame;
			
			App.S.PushBlocking(
				done => App.Scenes.Request(SceneRequest.Load(result => done(), Scenes))	
			);
		}
		#endregion

		#region Idle
		protected override void Idle()
		{
			// GenerateNewGame(
			// 	result =>
			// 	{
			// 		if (result.Status != ResultStatus.Success)
			// 		{
			// 			result.Log("Generating new game did not succeed!");
			// 			return;
			// 		}
			// 		
			// 		App.S.RequestState(
			// 			new GamePayload
			// 			{
			// 				Preferences = Payload.Preferences,
			// 				Game = result.Payload
			// 			}
			// 		);
			// 	}
			// );
			
		}
		#endregion
		
		#region End
		protected override void End()
		{
			Payload.MainMenu.StartGame -= OnMainMenuStartGame;
			Payload.MainMenu.CreateGame -= OnCreateGame;
			
			App.S.PushBlocking(
				done => App.P.UnRegisterAll(done)
			);

			App.S.PushBlocking(
				done => App.Scenes.Request(SceneRequest.UnLoad(result => done(), Scenes))
			);
		}
		#endregion

		#region Events
		void OnCreateGame(Action<Result<GameModel>> done)
		{
			// if (!string.IsNullOrEmpty(Payload.AutoLoadGame))
			// {
			// 	App.M.Load<GameModel>(
			// 		Payload.AutoLoadGame,
			// 		result =>
			// 		{
			// 			result.Log();
			//
			// 			Debug.Log(result.TypedModel.Rooms.All.Value.Active.Length);
			//
			// 			if (result.Status == ResultStatus.Success)
			// 			{
			// 				done(Result<GameModel>.Success(result.TypedModel));
			// 				return;
			// 			}
			// 		}
			// 	);
			// 	return;
			// }

			var game = App.M.Create<GameModel>(App.M.CreateUniqueId());

			game.LevelGeneration.Seed.Value = DemonUtility.NextInteger;
			
			game.DesireDamageMultiplier.Value = 1f;
			game.SimulationTimeConversion.Value = 1f / 120f; // 1f / [ REAL SECONDS PER DAY HERE ]
			
			game.Effects.IsEnabled.Value = true;
			game.Toolbar.IsEnabled.Value = true;
			
			game.WorldCamera.IsEnabled.Value = true;
			game.WorldCamera.PanVelocity.Value = 12f;
			game.WorldCamera.OrbitVelocity.Value = 64f;
			
			game.RoomResolver.RoomCountMinimum.Value = 4;
			game.RoomResolver.RoomCountMaximum.Value = 8;

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
			
			done(Result<GameModel>.Success(game));
		}
		
		void OnMainMenuStartGame(GameModel game)
		{
			App.S.RequestState(
				new GamePayload
				{
					Game = game
				}
			);
		}
		#endregion
	}
}