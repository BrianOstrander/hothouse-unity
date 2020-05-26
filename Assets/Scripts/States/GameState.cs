using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Presenters;
using Lunra.StyxMvp;
using Lunra.StyxMvp.Services;
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
			App.S.PushBlocking(OnBeginLoadScenes);
			App.S.PushBlocking(OnBeginInstantiatePresenters);
			App.S.PushBlocking(OnBeginInitializeLighting);
			App.S.PushBlocking(
				OnBeginInitializeNavigationMesh,
				() => Payload.Game.NavigationMesh.CalculationState.Value == NavigationMeshModel.CalculationStates.Completed
			);
			App.S.Push(Payload.Game.TriggerSimulationInitialize);
			App.S.PushBlocking(OnBeginInitializeCache);
		}

		void OnBeginLoadScenes(Action done)
		{
			App.Scenes.Request(
				SceneRequest.Load(
					result => done(),
					Scenes
				)
			);
		}

		void OnBeginInstantiatePresenters(Action done)
		{
			new NavigationMeshPresenter(Payload.Game);
			
			new GameResultPresenter(Payload.Game, Payload.Preferences);
			new GameInteractionPresenter(Payload.Game.Interaction);
			
			new WorldCameraPresenter(Payload.Game);
			new ToolbarPresenter(Payload.Game);
			new FloraEffectsPresenter(Payload.Game);

			new HintsPresenter(Payload.Game);
			new BuildWidgetPresenter(Payload.Game);

			Payload.Game.Rooms.Initialize(m => new RoomPrefabPresenter(Payload.Game, m));
			Payload.Game.Doors.Initialize(m => new DoorPrefabPresenter(Payload.Game, m));
			Payload.Game.Buildings.Initialize(Payload.Game);
			
			Payload.Game.Debris.Initialize(Payload.Game);
			Payload.Game.Flora.Initialize(Payload.Game);
			Payload.Game.ItemDrops.Initialize(m => new ItemDropPresenter(Payload.Game, m));
			Payload.Game.Dwellers.Initialize(Payload.Game);
			
			done();
		}

		void OnBeginInitializeNavigationMesh() => Payload.Game.NavigationMesh.TriggerInitialize();

		void OnBeginInitializeLighting(Action done)
		{
			switch (Payload.Game.LastLightUpdate.Value.State)
			{
				case LightDelta.States.Calculated:
					done();
					return;
				case LightDelta.States.Unknown:
					Payload.Game.LastLightUpdate.Value = Payload.Game.LastLightUpdate.Value.SetRoomStale(
						Payload.Game.Rooms.AllActive.Select(r => r.Id.Value).ToArray()
					);
					break;
			}
			CalculateLighting();

			done();
		}
		
		void OnBeginInitializeCache(Action done)
		{
			Payload.Game.InitializeCache(); 
			
			done();
		}
		#endregion

		#region Idle
		protected override void Idle()
		{
			Payload.Game.CalculateMaximumLighting = OnCalculateMaximumLighting;
			
			App.Heartbeat.Update += OnHeartbeatUpdate;
			App.Heartbeat.LateUpdate += OnHeartbeatLateUpdate;
			
			Payload.Game.SimulationMultiplier.Changed += OnGameSimulationMultiplier;
			OnGameSimulationMultiplier(Payload.Game.SimulationMultiplier.Value);
			// App.Heartbeat.Wait(
			// 	() =>
			// 	{
			// 		Debug.Log("Recalculating...");
			// 		Payload.Game.Doors.FirstActive().IsOpen.Value = true;
			// 		// Payload.Game.Dwellers.AllActive.First(d => d.Id.Value == "0").Health.Value = 0f;
			// 	},
			// 	6f
			// );
		}

		void OnHeartbeatUpdate()
		{
			Payload.Game.SimulationTime.Value += new DayTime(Payload.Game.SimulationTimeDelta);
			Payload.Game.SimulationUpdate();
			
			Payload.Game.SimulationPlaytimeElapsed.Value += TimeSpan.FromSeconds(Time.deltaTime);
			Payload.Game.PlaytimeElapsed.Value += TimeSpan.FromSeconds(Time.unscaledDeltaTime);

			switch (Payload.Game.LastLightUpdate.Value.State)
			{
				case LightDelta.States.Calculated:
					break;
				case LightDelta.States.Stale:
					CalculateLighting();
					break;
				default:
					Debug.LogError("Unrecognized LightingState: "+Payload.Game.LastLightUpdate.Value.State);
					break;
			}

			Payload.Game.CalculateCache();
		}

		void OnHeartbeatLateUpdate()
		{
			if (Payload.Game.GameResult.Value.State == GameResult.States.Unknown) return;
			
			Payload.Game.SimulationMultiplier.Changed -= OnGameSimulationMultiplier;
			
			App.Heartbeat.Update -= OnHeartbeatUpdate;
			App.Heartbeat.LateUpdate -= OnHeartbeatLateUpdate;
			
			Payload.Game.CalculateMaximumLighting = null;
			
			App.S.RequestState(
				new MainMenuPayload
				{
					Preferences = Payload.Preferences
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
		
		#region GameModel Events
		void OnGameSimulationMultiplier(float multiplier)
		{
			Time.timeScale = multiplier;
		}
		#endregion
		
		#region Utility
		void CalculateLighting()
		{
			Dictionary<string, List<RoomPrefabModel>> roomMap;
			IEnumerable<ILightSensitiveModel> lightSensitives;
			
			if (Payload.Game.LastLightUpdate.Value.SensitiveIds.Any())
			{
				lightSensitives = Payload.Game.LightSensitives.Where(l => Payload.Game.LastLightUpdate.Value.SensitiveIds.Contains(l.Id.Value));
				roomMap = Payload.Game.GetOpenAdjacentRoomsMap(
					Payload.Game.LastLightUpdate.Value.RoomIds
						.Union(lightSensitives.Select(l => l.RoomId.Value))
						.ToArray()
				);

				if (Payload.Game.LastLightUpdate.Value.RoomIds.Any()) lightSensitives = Payload.Game.LightSensitives;
			}
			else
			{
				roomMap = Payload.Game.GetOpenAdjacentRoomsMap(Payload.Game.LastLightUpdate.Value.RoomIds);
				lightSensitives = Payload.Game.LightSensitives;
			}

			var allRooms = roomMap.Values.SelectMany(v => v).Distinct();
			var allLights = Payload.Game.Lights.Where(l => allRooms.Any(r => r.RoomId.Value == l.RoomId.Value)).ToList();

			foreach (var lightSensitive in lightSensitives)
			{
				if (!roomMap.TryGetValue(lightSensitive.RoomId.Value, out var rooms)) continue;

				lightSensitive.LightLevel.Value = OnCalculateMaximumLighting(
					lightSensitive.Position.Value,
					allLights.Where(l => rooms.Any(r => r.RoomId.Value == l.RoomId.Value))
				);
			}

			Payload.Game.LastLightUpdate.Value = LightDelta.Calculated();
		}

		float OnCalculateMaximumLighting(
			(string RoomId, Vector3 Position) roomPosition
		)
		{
			var rooms = Payload.Game.GetOpenAdjacentRooms(roomPosition.RoomId);

			return OnCalculateMaximumLighting(
				roomPosition.Position,
				Payload.Game.Lights.Where(l => rooms.Any(r => r.RoomId.Value == l.RoomId.Value))
			);
		}
		
		float OnCalculateMaximumLighting(
			Vector3 position,
			IEnumerable<ILightModel> lights
		)
		{
			var result = 0f;

			foreach (var light in lights)
			{
				if (light.IsLightNotActive()) continue;

				var distance = Vector3.Distance(position, light.Position.Value);

				if (light.LightRange.Value <= distance) continue;

				result = Mathf.Max(
					result,
					1f - (distance / light.LightRange.Value)
				);
			}

			return result;
		}
		#endregion
	}
}