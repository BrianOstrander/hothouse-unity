using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Presenters;
using Lunra.Hothouse.Views;
using Lunra.StyxMvp;
using Lunra.StyxMvp.Presenters;
using Lunra.StyxMvp.Services;
using Lunra.Hothouse.Models.AgentModels;
using UnityEngine;

namespace Lunra.Hothouse.Services
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
			App.S.PushBlocking(OnBeginInstantiatePresenters);
			App.S.PushBlocking(
				OnBeginInitializeNavigationMesh,
				() => Payload.Game.NavigationMesh.CalculationState.Value == NavigationMeshModel.CalculationStates.Completed
			);
			App.S.Push(Payload.Game.TriggerSimulationInitialize);
		}

		void OnBeginInstantiatePresenters(Action done)
		{
			new NavigationMeshPresenter(Payload.Game);
			
			Payload.Game.FloraEffects.IsEnabled.Value = true; // This should probably be true by default or on init...

			new GameResultPresenter(Payload.Game, Payload.Preferences);
			new GenericPresenter<EventSystemView>().Show();
			
			new WorldCameraPresenter(Payload.Game);
			new SelectionPresenter(Payload.Game);
			new FloraEffectsPresenter(Payload.Game);

			Payload.Game.Rooms.Initialize(m => new RoomPrefabPresenter(Payload.Game, m));
			Payload.Game.Doors.Initialize(m => new DoorPrefabPresenter(Payload.Game, m));
			Payload.Game.Buildings.Initialize(m => new BuildingPresenter(Payload.Game, m));
			
			Payload.Game.Debris.Initialize(m => new ClearablePresenter<ClearableModel, ClearableView>(Payload.Game, m));
			Payload.Game.Flora.Initialize(m => new FloraPresenter(Payload.Game, m));
			Payload.Game.ItemDrops.Initialize(m => new ItemDropPresenter(Payload.Game, m));
			Payload.Game.Dwellers.Initialize(m => new DwellerPresenter(Payload.Game, m));
			
			done();
		}

		void OnBeginInitializeNavigationMesh() => Payload.Game.NavigationMesh.TriggerInitialize();
		#endregion

		#region Idle
		protected override void Idle()
		{
			Payload.Game.SimulationMultiplier.Changed += OnGameSimulationMultiplier;
			
			App.Heartbeat.Update += OnHeartbeatUpdate;

			App.Heartbeat.Wait(
				() =>
				{
					Debug.Log("Recalculating...");
					Payload.Game.Doors.FirstActive().IsOpen.Value = true;
					// Payload.Game.Dwellers.AllActive.First(d => d.Id.Value == "0").Health.Value = 0f;
				},
				6f
			);
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
		
		#region Utility
		
		#endregion
	}
}