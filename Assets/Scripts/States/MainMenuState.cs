﻿using System;
using Lunra.Core;
using Lunra.StyxMvp;
using Lunra.StyxMvp.Services;
using Lunra.WildVacuum.Models;
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
			
			var flora0 = new FloraModel();
			flora0.RoomId.Value = room0.Id.Value;
			flora0.IsEnabled.Value = true;
			flora0.Position.Value = new Vector3(7f, 0f, -5f);
			flora0.Rotation.Value = Quaternion.identity;
			flora0.Age.Value = FloraModel.Interval.Create(0.1f);
			flora0.ReproductionElapsed.Value = FloraModel.Interval.Create(1f);
			flora0.ReproductionRadius.Value = new FloatRange(0.5f, 1f);
			flora0.ReproductionFailureLimit.Value = 30;
			flora0.HealthMaximum.Value = 100f;
			flora0.Health.Value = flora0.HealthMaximum.Value;

			var flora1 = new FloraModel();
			flora1.RoomId.Value = room0.Id.Value;
			flora1.IsEnabled.Value = true;
			flora1.Position.Value = new Vector3(-12f, -0.8386866f, 6f);
			flora1.Rotation.Value = Quaternion.identity;
			flora1.Age.Value = flora0.Age.Value;
			flora1.ReproductionElapsed.Value = flora0.ReproductionElapsed.Value;
			flora1.ReproductionRadius.Value = flora0.ReproductionRadius.Value;
			flora1.ReproductionFailureLimit.Value = flora0.ReproductionFailureLimit.Value;
			flora1.HealthMaximum.Value = flora0.HealthMaximum.Value;
			flora1.Health.Value = flora0.Health.Value;
			
			game.Flora.Value = new[]
			{
				flora0,
				flora1
			};

			done(Result<GameModel>.Success(game));
		}
	}
}