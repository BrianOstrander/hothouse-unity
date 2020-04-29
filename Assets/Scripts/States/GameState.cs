using System;
using System.Linq;
using Lunra.Core;
using Lunra.StyxMvp;
using Lunra.StyxMvp.Presenters;
using Lunra.StyxMvp.Services;
using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Presenters;
using Lunra.WildVacuum.Views;
using UnityEngine;

namespace Lunra.WildVacuum.Services
{
	public class GamePayload : IStatePayload
	{
		public PreferencesModel Preferences;
		public GameModel Game;
	}

	public class GameState : State<GamePayload>
	{
		// Reminder: Keep variables in payload for easy reset of states!

		static string[] Scenes => new []
		{
			"Game"
		};
		
		#region Begin
		protected override void Begin()
		{
			App.S.PushBlocking(
				done => App.Scenes.Request(SceneRequest.Load(result => done(), Scenes))    
			);
			App.S.PushBlocking(OnBeginInitializePresenters);
			App.S.Push(Payload.Game.TriggerSimulationInitialize);
		}

		void OnBeginInitializePresenters(Action done)
		{
			Payload.Game.FloraEffects.IsEnabled.Value = true;
			
			new WorldCameraPresenter(Payload.Game);
			new SelectionPresenter(Payload.Game);
			new FloraEffectsPresenter(Payload.Game);

			foreach (var room in Payload.Game.Rooms.Value) new RoomPrefabPresenter(Payload.Game, room);

			foreach (var door in Payload.Game.Doors.Value) new DoorPrefabPresenter(Payload.Game, door);

			OnGameFlora(Payload.Game.Flora.Value);

			done();
		}
		#endregion

		#region Idle
		protected override void Idle()
		{
			App.Heartbeat.Update += OnHeartbeatUpdate;

			Payload.Game.Flora.Changed += OnGameFlora;
		}

		void OnHeartbeatUpdate(float delta)
		{
			Payload.Game.SimulationUpdate(delta * Payload.Game.SimulationUpdateMultiplier.Value);
		}
		#endregion
        
		#region End
		protected override void End()
		{
			App.Heartbeat.Update -= OnHeartbeatUpdate;
			
			Payload.Game.Flora.Changed -= OnGameFlora;
			
			App.S.PushBlocking(
				done => App.P.UnRegisterAll(done)
			);

			App.S.PushBlocking(
				done => App.Scenes.Request(SceneRequest.UnLoad(result => done(), Scenes))
			);
		}
		#endregion
		
		#region GameModel Events
		void OnGameFlora(FloraModel[] allFlora)
		{
			foreach (var flora in allFlora.Where(f => !f.HasPresenter.Value))
			{
				flora.HasPresenter.Value = true;
				new FloraPresenter(Payload.Game, flora);
			}
			Payload.Game.LastNavigationCalculation.Value = DateTime.Now;
		}
		#endregion
	}
}