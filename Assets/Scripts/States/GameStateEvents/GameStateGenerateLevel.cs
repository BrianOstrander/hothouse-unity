using System;
using System.Linq;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Presenters;
using Lunra.StyxMvp;
using UnityEngine;

namespace Lunra.Hothouse.Services.GameStateEvents
{
	public class GameStateGenerateLevel
	{
		GameState state;
		GamePayload payload;

		RoomModel oldExit;
		
		public GameStateGenerateLevel(GameState state)
		{
			this.state = state;
			payload = state.Payload;
		}

		public void Push()
		{
			payload.Game.IsSimulating.Value = false;
			payload.Game.ResetSimulationInitialized();
			
			App.S.PushBlocking(OnCleanup);
			
			App.S.PushBlocking(OnGenerateRooms);
			App.S.PushBlocking(OnTransferRooms);
			App.S.PushBlocking(OnInitializeLighting);
			App.S.PushBlocking(
				() => payload.Game.NavigationMesh.QueueCalculation(),
				() => payload.Game.NavigationMesh.CalculationState.Value == NavigationMeshModel.CalculationStates.Completed
			);
			
			App.S.Push(() => payload.Game.IsSimulating.Value = true);
			App.S.Push(payload.Game.TriggerSimulationInitialize);
		}

		void OnCleanup(Action done)
		{
			oldExit = payload.Game.Rooms.AllActive.FirstOrDefault(r => r.IsExit.Value);

			if (oldExit == null)
			{
				done();
				return;
			}

			// HACK BEGIN
			foreach (var dweller in payload.Game.Dwellers.AllActive)
			{
				dweller.RoomTransform.Id.Value = oldExit.RoomTransform.Id.Value;
				dweller.Transform.Position.Value = oldExit.Transform.Position.Value;
			}

			oldExit.IsRevealed.Value = true;
			// HACK END
			
			var offset = Vector3.up * 100f;

			oldExit.Transform.Position.Value += offset;
			
			foreach (var dweller in payload.Game.Dwellers.AllActive)
			{
				if (dweller.RoomTransform.Id.Value != oldExit.RoomTransform.Id.Value)
				{
					dweller.PooledState.Value = PooledStates.InActive;
					continue;
				}

				dweller.Transform.Position.Value += offset;
			}
			
			foreach (var building in payload.Game.Buildings.AllActive)
			{
				if (building.RoomTransform.Id.Value != oldExit.RoomTransform.Id.Value)
				{
					building.PooledState.Value = PooledStates.InActive;
					continue;
				}

				building.Transform.Position.Value += offset;
			}

			payload.Game.WorldCamera.Transform.Position.Value = oldExit.Transform.Position.Value;
			
			payload.Game.ItemDrops.InActivateAll();
			payload.Game.Doors.InActivateAll();
			payload.Game.Debris.InActivateAll();
			payload.Game.Flora.InActivateAll();
			payload.Game.ObligationIndicators.InActivateAll();

			payload.Game.Rooms.InActivate(
				payload.Game.Rooms.AllActive.Where(r => r.Id.Value != oldExit.Id.Value).ToArray()
			);

			App.Heartbeat.WaitForSeconds(done, 5f);
			// App.Heartbeat.WaitForFixedUpdate(done);
		}

		void OnGenerateRooms(Action done)
		{
			payload.Game.RoomResolver.Generate(done);
		}

		void OnTransferRooms(Action done)
		{
			if (oldExit == null)
			{
				done();
				return;
			}

			var spawn = payload.Game.Rooms.AllActive.First(r => r.IsSpawn.Value);

			spawn.IsRevealed.Value = true;
			
			foreach (var dweller in payload.Game.Dwellers.AllActive)
			{
				dweller.RoomTransform.Id.Value = spawn.RoomTransform.Id.Value;
				dweller.Transform.Position.Value = spawn.Transform.Position.Value;
			}

			oldExit.PooledState.Value = PooledStates.InActive;

			done();
		}

		void OnInitializeLighting(Action done)
		{
			payload.Game.LastLightUpdate.Value = payload.Game.LastLightUpdate.Value.SetRoomStale(
				payload.Game.Rooms.AllActive.Select(r => r.Id.Value).ToArray()
			);
			state.CalculateLighting();
			
			done();
		}
	}
}