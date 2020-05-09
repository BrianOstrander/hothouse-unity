﻿using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.StyxMvp;
using Lunra.StyxMvp.Presenters;
using Lunra.StyxMvp.Services;
using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Models.AgentModels;
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

			new GameResultPresenter(Payload.Game, Payload.Preferences);
			new GenericPresenter<EventSystemView>().Show();
			
			new WorldCameraPresenter(Payload.Game);
			new SelectionPresenter(Payload.Game);
			new FloraEffectsPresenter(Payload.Game);

			Payload.Game.Rooms.Initialize(m => new RoomPrefabPresenter(Payload.Game, m));
			Payload.Game.Doors.Initialize(m => new DoorPrefabPresenter(Payload.Game, m));
			Payload.Game.Buildings.Initialize(m => new BuildingPresenter(Payload.Game, m));
			
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
			// 		Debug.Log("Killing dweller!");
			// 		Payload.Game.Dwellers.AllActive.First().Health.Value = 0f;
			// 	},
			// 	10f
			// );
		}

		void OnHeartbeatUpdate()
		{
			Payload.Game.SimulationTime.Value += new DayTime(Payload.Game.SimulationTimeDelta);
			Payload.Game.SimulationUpdate();
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