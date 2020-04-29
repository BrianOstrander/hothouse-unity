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
			/*
			var p = new ModelPool<FloraModel>();

			void printPool(string notes)
			{
				var result = "> " + notes + "\n\tActive:\n";
				foreach (var entry in p.GetActive()) result += "\t\t- " + entry.Id.Value + "\n";
				result += "\tInActive:\n";
				foreach (var entry in p.GetInActive()) result += "\t\t- " + entry.Id.Value + "\n";
				Debug.Log(result);
			}
			
			printPool("Nothing");
			
			p.Activate(f => f.Id.Value = "A");
			p.Activate(f => f.Id.Value = "B");
			p.Activate(f => f.Id.Value = "C");
			
			printPool("All Active");
			
			var fA = p.GetActive().First(f => f.Id.Value == "A");
			var fB = p.GetActive().First(f => f.Id.Value == "B");
			var fC = p.GetActive().First(f => f.Id.Value == "C");
			
			p.InActivate(fA);
			
			printPool("A is now InActive");
			
			p.Activate();
			
			printPool("A should now be Active again");
			
			Debug.Break();
			*/
			
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

			foreach (var flora in Payload.Game.Flora.GetActive()) new FloraPresenter(Payload.Game, flora);

			done();
		}
		#endregion

		#region Idle
		protected override void Idle()
		{
			App.Heartbeat.Update += OnHeartbeatUpdate;
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
			
			App.S.PushBlocking(
				done => App.P.UnRegisterAll(done)
			);

			App.S.PushBlocking(
				done => App.Scenes.Request(SceneRequest.UnLoad(result => done(), Scenes))
			);
		}
		#endregion
		
		#region GameModel Events
		#endregion
	}
}