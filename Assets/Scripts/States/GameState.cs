﻿using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Presenters;
using Lunra.Hothouse.Services.GameStateEvents;
using Lunra.StyxMvp;
using Lunra.StyxMvp.Models;
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
			App.S.PushBlocking(
				OnBeginInitializeNavigationMesh,
				() => Payload.Game.NavigationMesh.CalculationState.Value == NavigationMeshModel.CalculationStates.Completed
			);
			
			App.S.PushBlocking(Payload.Game.DwellerNames.Initialize);

			if (Payload.Game.Rooms.AllActive.None())				
			{
				App.S.PushBlocking(done => Payload.Game.RoomResolver.Initialize(done));
				new GameStateGenerateLevel(this).Push();
			}
			
			// App.S.PushBreak();
			App.S.Push(Payload.Game.TriggerSimulationInitialize);

			App.S.PushBlocking(OnBeginInitializeCache);
			
			App.S.Push(OnBeginInitializeServices);
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
			new EffectsPresenter(Payload.Game);

			// new HintsPresenter(Payload.Game);
			new BuildValidationPresenter(Payload.Game);
			new GlobalInventoryCounterPresenter(Payload.Game);
			new JobManagePresenter(Payload.Game);
			new BuildingManagePresenter(Payload.Game);

			new RoomResolverPresenter(Payload.Game);
			
			Payload.Game.Buildings.Initialize(Payload.Game);
			Payload.Game.Rooms.Initialize(Payload.Game);
			Payload.Game.Doors.Initialize(Payload.Game);
			Payload.Game.Debris.Initialize(Payload.Game);
			Payload.Game.Flora.Initialize(Payload.Game);
			Payload.Game.ItemDrops.Initialize(Payload.Game);
			Payload.Game.Dwellers.Initialize(Payload.Game);
			Payload.Game.Bubblers.Initialize(Payload.Game);
			Payload.Game.SnapCaps.Initialize(Payload.Game);
			Payload.Game.Decorations.Initialize(Payload.Game);
			Payload.Game.Generators.Initialize(Payload.Game);
			
			done();
		}
		
		void OnBeginInitializeNavigationMesh() => Payload.Game.NavigationMesh.TriggerInitialize();

		void OnBeginInitializeCache(Action done)
		{
			Payload.Game.InitializeCache(); 
			
			done();
		}

		void OnBeginInitializeServices()
		{
			new PopulationService(Payload.Game);
			new HostileSpawnService(Payload.Game);
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
			
			// App.Heartbeat.WaitForSeconds(
			// 	() =>
			// 	{
			// 		Debug.Log("Selecting building...");
			// 		Payload.Game.BuildingManage.Selection.Value = Payload.Game.Buildings.AllActive.First();
			// 	},
			// 	5f
			// );
		}

		void OnHeartbeatUpdate()
		{
			if (Input.GetKeyUp(KeyCode.Alpha1))
			{
				Payload.Game.SimulationMultiplier.Value = 1f;
			}
			else if (Input.GetKeyUp(KeyCode.Alpha2))
			{
				Payload.Game.SimulationMultiplier.Value = 4f;
			}
			else if (Input.GetKeyUp(KeyCode.Alpha3))
			{
				Payload.Game.SimulationMultiplier.Value = 8f;
			}
			else if (Input.GetKeyUp(KeyCode.Space))
			{
				if (Payload.Game.IsSimulating) Payload.Game.SimulationMultiplier.Value = 0f;
				else Payload.Game.SimulationMultiplier.Value = Payload.Game.LastNonZeroSimulationMultiplier.Value;
			}
			
			if (!Payload.Game.IsSimulating) return;
			
			Payload.Game.StepSimulation(Time.deltaTime);
			
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

			// var d = Payload.Game.Dwellers.FirstActive();
			// d.Goals.Apply(
			// 	(Motives.Heal, 0.1f * Payload.Game.SimulationTimeDelta)
			// );
		}

		void OnHeartbeatLateUpdate()
		{
			if (Payload.Game.GameResult.Value.State != GameResult.States.Failure) return;
			
			App.S.RequestState<MainMenuPayload>();
		}
		#endregion
        
		#region End
		protected override void End()
		{
			Payload.Game.SimulationMultiplier.Changed -= OnGameSimulationMultiplier;
			
			App.Heartbeat.Update -= OnHeartbeatUpdate;
			App.Heartbeat.LateUpdate -= OnHeartbeatLateUpdate;

			Payload.Game.CalculateMaximumLighting = null;
			
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
		public void CalculateLighting()
		{
			// TODO: This seems messy, ideally we should calculate only things sensitive to light, and if a room has
			// been set stale, assume it's because a light was placed in there and calculate all sensitive objects in
			// and around that room.
			
			IEnumerable<RoomModel> rooms;
			IEnumerable<ILightSensitiveModel> lightSensitives;

			rooms = Payload.Game.Rooms.AllActive.Where(room => room.IsRevealed.Value);

			if (Payload.Game.LastLightUpdate.Value.RoomIds.None() && Payload.Game.LastLightUpdate.Value.SensitiveIds.Any())
			{
				lightSensitives = Payload.Game.Query.All<ILightSensitiveModel>()
					.Where(lightSensitive => Payload.Game.LastLightUpdate.Value.SensitiveIds.Contains(lightSensitive.Id.Value));
			}
			else
			{
				lightSensitives = Payload.Game.Query.All<ILightSensitiveModel>()
					.Where(
						lightSensitive => rooms.Any(
							r =>
							{
								if (lightSensitive.RoomTransform.Id.Value == r.RoomTransform.Id.Value) return true;
								if (!lightSensitive.LightSensitive.HasConnectedRoomId) return false;
								return lightSensitive.LightSensitive.ConnectedRoomId.Value == r.RoomTransform.Id.Value;
							}
						)
					);	
			}
			
			var allLights = Payload.Game.Query.All<ILightModel>(m => m.Light.IsLightActive())
				.Where(light => rooms.Any(room => room.RoomTransform.Id.Value == light.RoomTransform.Id.Value))
				.ToList();
			
			foreach (var lightSensitive in lightSensitives)
			{
				lightSensitive.LightSensitive.LightLevel.Value = OnCalculateMaximumLighting(
					lightSensitive.Transform.Position.Value,
					allLights
				);
			}

			Payload.Game.LastLightUpdate.Value = LightDelta.Calculated();
		}

		LightingResult OnCalculateMaximumLighting(
			(
				string RoomId,
				Vector3 Position,
				ILightModel[] Except
			)
				request
		)
		{
			var result = new LightingResult();

			var rooms = Payload.Game.Rooms.AllActive.Where(r => r.IsRevealed.Value);

			bool isLightNotExceptedAndInRoom(ILightModel light)
			{
				if (request.Except != null  && request.Except.Any(l => l.Id.Value == light.Id.Value)) return false;
				return rooms.Any(r => r.RoomTransform.Id.Value == light.RoomTransform.Id.Value);
			}
			
			result.OperatingMaximum = OnCalculateMaximumLighting(
				request.Position,
				Payload.Game.Query.All<ILightModel>(m => m.Light.IsLightActive() && isLightNotExceptedAndInRoom(m))
			);

			result.ConstructingMaximum = OnCalculateMaximumLighting(
				request.Position,
				Payload.Game.Query.All<ILightModel>(
					l =>
					{
						if (!l.Light.IsLight.Value) return false;
						if (l.Light.IsLightActive()) return false;
						
						switch (l.Light.LightState.Value)
						{
							case LightStates.Fueled:
							case LightStates.Extinguishing:
								break;
							case LightStates.Extinguished:
								return false;
							default:
								Debug.LogError("Unrecognized LightState: "+l.Light.LightState.Value);
								return false;
						}

						return isLightNotExceptedAndInRoom(l);
					}
				)
			);

			return result;
		}
		
		float OnCalculateMaximumLighting(
			Vector3 position,
			IEnumerable<ILightModel> lights
		)
		{
			var result = 0f;

			foreach (var light in lights)
			{
				var distance = Vector3.Distance(position, light.Transform.Position.Value);

				if (light.Light.LightRange.Value <= distance) continue;

				result = Mathf.Max(
					result,
					1f - (distance / light.Light.LightRange.Value)
				);
			}

			return result;
		}
		#endregion
	}
}