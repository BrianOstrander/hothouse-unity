using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.StyxMvp;
using Lunra.StyxMvp.Services;
using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Presenters;
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

			foreach (var model in Payload.Game.Rooms.Value) new RoomPrefabPresenter(Payload.Game, model);
			foreach (var model in Payload.Game.Doors.Value) new DoorPrefabPresenter(Payload.Game, model);
			foreach (var model in Payload.Game.ItemCaches.Value) new ItemCacheBuildingPresenter(Payload.Game, model);
			
			Payload.Game.Flora.Initialize(m => new FloraPresenter(Payload.Game, m));
			Payload.Game.ItemDrops.Initialize(m => new ItemDropPresenter(Payload.Game, m));
			Payload.Game.Dwellers.Initialize(m => new DwellerPresenter(Payload.Game, m));
			
			done();
		}
		#endregion

		#region Idle
		protected override void Idle()
		{
			Payload.Game.SimulationMultiplier.Changed += OnGameSimulationMultiplier;
			
			App.Heartbeat.Update += OnHeartbeatUpdate;

			// App.Heartbeat.Wait(
			// 	() =>
			// 	{
			// 		Payload.Game.ItemCaches.Value.First().Inventory.Value = Inventory.PopulateMaximum(
			// 			new Dictionary<Item.Types, int>
			// 			{
			// 				{ Item.Types.Stalks, 999 }
			// 			}
			// 		);
			// 	},
			// 	20f
			// );
		}

		void OnHeartbeatUpdate()
		{
			Payload.Game.SimulationTime.Value += new DayTime(Payload.Game.SimulationTimeDelta);
			Payload.Game.SimulationUpdate();
			
			// Debug.Log(Payload.Game.SimulationTime.Value.ToDayTimeString());
		}
		#endregion
        
		#region End
		protected override void End()
		{
			Payload.Game.SimulationMultiplier.Changed -= OnGameSimulationMultiplier;
			
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
		void OnGameSimulationMultiplier(float multiplier)
		{
			Time.timeScale = multiplier;
		}
		#endregion
	}
}