using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.NumberDemon;
using Lunra.StyxMvp;
using UnityEngine;
using UnityEngine.AI;

namespace Lunra.Hothouse.Services.GameStateEvents
{
	public class GameStateGenerateLevel
	{
		GameState state;
		GamePayload payload;
		Demon generator;
		RoomResolverRequest request;
		
		DateTime beginTime;

		RoomModel spawn;
		
		public GameStateGenerateLevel(GameState state)
		{
			this.state = state;
			payload = state.Payload;
			generator = new Demon(1);
			request = RoomResolverRequest.Default(
				generator,
				payload.Game.Rooms.Activate,
				payload.Game.Doors.Activate
			);
		}

		public void Push()
		{
			App.S.Push(OnBegin);

			App.S.PushBlocking(OnGenerateRooms);
			App.S.PushBlocking(OnGenerateSpawn);
			
			App.S.PushBlocking(OnGenerateFloraBegin);
			App.S.PushBlocking(OnGenerateFloraSeed);
			
			App.S.PushBlocking(OnGenerateHostiles);
			
			App.S.PushBlocking(OnGenerateDwellers);
			App.S.PushBlocking(OnGenerateStartingBuildings);
			
			// App.S.PushHalt();

			App.S.PushBlocking(OnRevealRooms);
			
			App.S.PushBlocking(OnInitializeLighting);
			// App.S.PushBlocking(
			// 	() => payload.Game.NavigationMesh.QueueCalculation(),
			// 	() => payload.Game.NavigationMesh.CalculationState.Value == NavigationMeshModel.CalculationStates.Completed
			// );
			
			App.S.Push(() => payload.Game.IsSimulating.Value = true);
			App.S.Push(payload.Game.TriggerSimulationInitialize);
			App.S.Push(OnEnd);
		}

		void OnBegin()
		{
			beginTime = DateTime.Now;
			payload.Game.IsSimulating.Value = false;
			payload.Game.ResetSimulationInitialized();
		}

		void OnGenerateRooms(Action done)
		{
			payload.Game.RoomResolver.Generate(
				request,
				result =>
				{
					done();
				}
			);
		}

		void OnGenerateSpawn(Action done)
		{
			var spawnOptions = payload.Game.Rooms.AllActive
				.Where(r => request.SpawnDoorCountRequired <= r.AdjacentRoomIds.Value.Count);
			
			spawn = generator.GetNextFrom(spawnOptions);

			if (spawn == null)
			{
				Debug.LogError("spawn is null, this should never happen");
				// TODO: Handle this error
			}

			spawn.IsSpawn.Value = true;
			spawn.SpawnDistance.Value = 0;

			var spawnDistanceMaximum = 0;
			var remainingSpawnDistanceChecks = new List<RoomModel>(new[] {spawn});

			while (remainingSpawnDistanceChecks.Any())
			{
				var next = remainingSpawnDistanceChecks[0];
				remainingSpawnDistanceChecks.RemoveAt(0);

				var neighboringRooms = payload.Game.Rooms.AllActive
					.Where(r => next.AdjacentRoomIds.Value.Keys.Any(k => r.Id.Value == k));

				foreach (var neighboringRoom in neighboringRooms)
				{
					if (neighboringRoom.SpawnDistance.Value == int.MaxValue) remainingSpawnDistanceChecks.Add(neighboringRoom);
					neighboringRoom.SpawnDistance.Value = Mathf.Min(next.SpawnDistance.Value + 1, neighboringRoom.SpawnDistance.Value);
				}

				spawnDistanceMaximum = Mathf.Max(spawnDistanceMaximum, next.SpawnDistance.Value);
			}
			
			done();
		}

		#region Generate Flora
		void OnGenerateFloraBegin(Action done)
		{
			foreach (var room in payload.Game.Rooms.AllActive) room.IsRevealed.Value = true;

			payload.Game.NavigationMesh.QueueCalculation();

			App.Heartbeat.WaitForCondition(
				done,
				() => payload.Game.NavigationMesh.CalculationState.Value == NavigationMeshModel.CalculationStates.Completed
			);
		}

		void OnGenerateFloraSeed(Action done)
		{
			var maximumSpawnDistance = (float)payload.Game.Rooms.AllActive.Max(r => r.SpawnDistance.Value);
			
			foreach (var room in payload.Game.Rooms.AllActive)
			{
				if (room.SpawnDistance.Value == 0) continue;

				var spawnDistanceNormalized = room.SpawnDistance.Value / maximumSpawnDistance;

				var availableFloraConstraints = request.FloraConstraints.Where(c => c.SpawnDistanceNormalizedMinimum <= spawnDistanceNormalized);
				
				if (availableFloraConstraints.None()) continue;
				
				var currentFloraConstraint = generator.GetNextFrom(availableFloraConstraints);

				var currentCount = 0;

				if (currentFloraConstraint.CountPerRoomMinimum == currentFloraConstraint.CountPerRoomMaximum) currentCount = currentFloraConstraint.CountPerRoomMinimum;
				else currentCount = generator.GetNextInteger(currentFloraConstraint.CountPerRoomMinimum, currentFloraConstraint.CountPerRoomMaximum + 1);
				
				if (currentCount == 0) continue;

				var floraBudgetRemaining = currentCount;
				var failureBudgetRemaining = floraBudgetRemaining * 2; // TODO: Don't hardcode this...

				while (0 < floraBudgetRemaining && 0 < failureBudgetRemaining)
				{
					var position = room.Boundary.RandomPoint(generator);
					if (!position.HasValue)
					{
						failureBudgetRemaining--;
						continue;
					}

					var sampleSuccess = NavMesh.SamplePosition(
						position.Value,
						out var hit,
						Mathf.Abs(position.Value.y) + 0.1f,
						NavMesh.AllAreas
					);

					if (!sampleSuccess || !room.Boundary.Contains(hit.position))
					{
						failureBudgetRemaining--;
						continue;
					}

					var collided = false;
					
					foreach (var possibleCollision in payload.Game.Flora.AllActive)
					{
						if (possibleCollision.RoomTransform.Id.Value != room.RoomTransform.Id.Value) continue;
						if (possibleCollision.ReproductionRadius.Value.Maximum < Vector3.Distance(possibleCollision.Transform.Position.Value, hit.position)) continue;

						collided = true;
						break;
					}

					if (collided)
					{
						failureBudgetRemaining--;
						continue;
					}
					
					// var collision = payload.Game.Flora.AllActive
					// 	.Any(f => f.RoomTransform.Id.Value == room.RoomTransform.Id && Vector3.Distance(f.Transform.Position.Value, hit.position) < ())
					
					payload.Game.Flora.ActivateAdult(
						currentFloraConstraint.Species,
						room.RoomTransform.Id.Value,
						hit.position
					);

					floraBudgetRemaining--;
				}
			}

			foreach (var flora in payload.Game.Flora.AllActive)
			{
				var constraint = request.FloraConstraints.First(c => c.Species == flora.Species.Value);

				var clusterCount = 1;

				if (constraint.CountPerClusterMinimum == constraint.CountPerClusterMaximum) clusterCount = constraint.CountPerClusterMinimum;
				else clusterCount = generator.GetNextInteger(constraint.CountPerClusterMinimum, constraint.CountPerClusterMaximum + 1);
				
				if (clusterCount <= 1) continue;

				var reproductionBudgetRemaining = clusterCount - 1;
				var failureBudgetRemaining = reproductionBudgetRemaining * 2;

				while (0 < reproductionBudgetRemaining && 0 < failureBudgetRemaining)
				{
					if (flora.TriggerReproduction(generator)) reproductionBudgetRemaining--;
					else failureBudgetRemaining--;
				}
			}
			
			done();	
		}
		#endregion

		void OnGenerateHostiles(Action done)
		{
			/*
			foreach (var room in payload.Game.Rooms.AllActive)
			{
				// TODO: Abstract this into a shared method...
				if (room.SpawnDistance.Value == 0) continue;
				
				var hostileBudgetRemaining = generator.GetNextInteger(0, 4);
				var failureBudgetRemaining = hostileBudgetRemaining * 2; // TODO: Don't hardcode this...

				while (0 < hostileBudgetRemaining && 0 < failureBudgetRemaining)
				{
					var position = room.Boundary.RandomPoint(generator);
					if (!position.HasValue)
					{
						failureBudgetRemaining--;
						continue;
					}

					var sampleSuccess = NavMesh.SamplePosition(
						position.Value,
						out var hit,
						Mathf.Abs(position.Value.y) + 0.1f,
						NavMesh.AllAreas
					);

					if (!sampleSuccess || !room.Boundary.Contains(hit.position))
					{
						failureBudgetRemaining--;
						continue;
					}
					
					payload.Game.Seekers.Activate(
						room.RoomTransform.Id.Value,
						hit.position
					);

					hostileBudgetRemaining--;
				}
			}
			*/
			
			done();
		}

		void OnRevealRooms(Action done)
		{
			foreach (var room in payload.Game.Rooms.AllActive)
			{
				room.IsRevealed.Value = room.IsSpawn.Value;
				room.RevealDistance.Value = room.IsRevealed.Value ? 0 : room.SpawnDistance.Value;
			}

			done();
		}

		void OnGenerateDwellers(Action done)
		{
			var randomJobPool = EnumExtensions.GetValues(Jobs.Unknown, Jobs.None, Jobs.Stoker);
			
			var requiredJobs = new Jobs[]
			{
				Jobs.Construction,
				Jobs.Clearer
			};

			for (var i = 0; i < 4; i++)
			{
				var dweller = payload.Game.Dwellers.Activate(
					spawn.Id.Value,
					spawn.Transform.Position.Value + (Vector3.forward * 2f)
				);

				if (i < requiredJobs.Length) dweller.Job.Value = requiredJobs[i];
				else dweller.Job.Value = generator.GetNextFrom(randomJobPool);

				dweller.Name.Value = payload.Game.DwellerNames.GetName(generator);
			}

			done();
		}

		void OnGenerateStartingBuildings(Action done)
		{	
			var bonfire = payload.Game.Buildings.Activate(
				Buildings.Bonfire,
				spawn.Id.Value,
				spawn.Transform.Position.Value + (Vector3.right * 2f),
				Quaternion.identity,
				BuildingStates.Operating
			);
			
			var wagon = payload.Game.Buildings.Activate(
				Buildings.StartingWagon,
				spawn.Id.Value,
				spawn.Transform.Position.Value + (Vector3.left * 2f),
				Quaternion.identity * Quaternion.Euler(0f, 90f, 0f),
				BuildingStates.Operating
			);

			wagon.Inventory.Value += (Inventory.Types.Stalks, 100);
			wagon.Inventory.Value += (Inventory.Types.Rations, 100);
			
			payload.Game.WorldCamera.Transform.Position.Value = bonfire.Transform.Position.Value;

			payload.Game.WorldCamera.Transform.Rotation.Value = Quaternion.LookRotation(
				new Vector3(
					-1f,
					0f,
					-1f
				).normalized,
				Vector3.up
			);
			
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
		
		void OnEnd()
		{
			var elapsedTime = DateTime.Now - beginTime;
			
			Debug.Log("Generated "+payload.Game.Rooms.AllActive.Length+" rooms in "+elapsedTime.TotalSeconds.ToString("N2")+" seconds for seed "+generator.Seed);
		}
	}
}