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
		
		RoomModel spawn;
		
		public GameStateGenerateLevel(GameState state)
		{
			this.state = state;
			payload = state.Payload;
			generator = new Demon(1);
			// generator = new Demon();
			request = RoomResolverRequest.Defaults.Tiny(
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
			
			App.S.PushBlocking(OnCalculateNavigation);
			
			App.S.PushBlocking(OnGenerateDebris);
			
			App.S.PushBlocking(OnCalculateNavigation);
			
			App.S.PushBlocking(OnGenerateFloraSeed);
			App.S.PushBlocking(OnGenerateHostiles);
			
			App.S.PushBlocking(OnGenerateDwellers);
			App.S.PushBlocking(OnGenerateStartingBuildings);
			App.S.PushBlocking(OnCleanupSpawn);
			
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
			payload.Game.GenerationLog.Append(GenerationEvents.Begin);
			payload.Game.IsSimulating.Value = false;
			payload.Game.ResetSimulationInitialized();
		}

		void OnGenerateRooms(Action done)
		{
			payload.Game.GenerationLog.Append(GenerationEvents.RoomGenerationBegin);
			payload.Game.RoomResolver.Generate(
				request,
				result =>
				{
					foreach (var room in payload.Game.Rooms.AllActive) room.IsRevealed.Value = true;
					payload.Game.GenerationLog.Append(GenerationEvents.RoomGenerationEnd);
					done();
				}
			);
		}

		void OnGenerateSpawn(Action done)
		{
			var spawnOptions = payload.Game.Rooms.AllActive
				.Where(r => r.PrefabTags.Value.Contains(PrefabTagCategories.Room.Spawn));
			
			var spawnOptionsPreferred = spawnOptions
				.Where(r => request.SpawnDoorCountRequired <= r.AdjacentRoomIds.Value.Count);

			spawn = generator.GetNextFrom(spawnOptionsPreferred.Any() ? spawnOptionsPreferred : spawnOptions);

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

			foreach (var room in payload.Game.Rooms.AllActive) room.SpawnDistanceNormalized.Value = room.SpawnDistance.Value / (float)spawnDistanceMaximum;
			
			payload.Game.GenerationLog.Append(GenerationEvents.SpawnChosen);
			done();
		}
		
		void OnGenerateDebris(Action done)
		{
			foreach (var room in payload.Game.Rooms.AllActive)
			{
				GenerateDebrisInRoom(
					room
				);
			}

			done();
		}

		#region Generate Flora
		void OnGenerateFloraSeed(Action done)
		{
			foreach (var room in payload.Game.Rooms.AllActive)
			{
				GenerateFloraInRoom(
					room,
					payload.Game.Flora.GetValidSpeciesData(room)	
				);
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
			var randomJobPool = EnumExtensions.GetValues(Jobs.Unknown, Jobs.None);
			
			var requiredJobs = new []
			{
				Jobs.Smoker,
				Jobs.Stockpiler,
				Jobs.Stockpiler,
				Jobs.Stockpiler,
				Jobs.Laborer
			};

			for (var i = 0; i < 5; i++)
			{
				var position = spawn.Transform.Position.Value + (Vector3.forward * (4f + i));

				if (Physics.Raycast(new Ray(position, Vector3.down), out var hit, 40f, LayerMasks.Floor))
				{
					position = hit.point;
				}
				
				var dweller = payload.Game.Dwellers.Activate(
					spawn.Id.Value,
					position
				);

				if (i < requiredJobs.Length) dweller.Job.Value = requiredJobs[i];
				else dweller.Job.Value = generator.GetNextFrom(randomJobPool);

				dweller.Name.Value = payload.Game.DwellerNames.GetName(generator);
				// dweller.IsDebugging = i == 0;
			}

			done();
		}

		void OnGenerateStartingBuildings(Action done)
		{
			var position = spawn.Transform.Position.Value;

			if (Physics.Raycast(new Ray(position, Vector3.down), out var hit, 40f, LayerMasks.Floor))
			{
				position = hit.point;
			}
			
			position += Vector3.back * 4f;
			
			var wagon = payload.Game.Buildings.Activate<StartingWagonDefinition>(
				spawn.Id.Value,
				position + (Vector3.left * 2f),
				Quaternion.identity * Quaternion.Euler(0f, 90f, 0f),
				BuildingStates.Operating
			);

			wagon.Inventory.Add(wagon.Inventory.AllCapacity.Value.GetMaximum());
		
			var bonfire = payload.Game.Buildings.Activate<BonfireLightDefinition>(
				spawn.Id.Value,
				position + (Vector3.right * 6f),
				Quaternion.identity,
				BuildingStates.Operating
			);

			var dryingRack = payload.Game.Buildings.Activate<DryingRackDefinition>(
				spawn.Id.Value,
				position + (Vector3.right * 3f) + (Vector3.back * 3f),
				Quaternion.identity, 
				BuildingStates.Operating
			);
			
			payload.Game.WorldCamera.Transform.Position.Value = bonfire.Transform.Position.Value;

			payload.Game.WorldCamera.Transform.Rotation.Value = Quaternion.LookRotation(
				new Vector3(
					-1f,
					0f,
					-1f
				).normalized,
				Vector3.up
			);
			
			// Debugging Begin
			wagon.Inventory.Remove(
				Inventory.FromEntries(
					wagon.Inventory.All.Value.Entries
						.Select(e => (e.Type, 1))
						.ToArray()
				)
			);
			// Debugging End
			
			done();
		}

		void OnCleanupSpawn(Action done)
		{
			var avoid = new Dictionary<IRoomTransformModel, float>();

			bool isInSpawn(IRoomTransformModel model) => model.RoomTransform.Id.Value == spawn.RoomTransform.Id.Value; 
			
			foreach (var model in payload.Game.Dwellers.AllActive) avoid.Add(model, 1f);
			foreach (var model in payload.Game.Buildings.AllActive.Where(isInSpawn)) avoid.Add(model, model.Boundary.Radius.Value);

			bool hasCollision(IRoomTransformModel model)
			{
				return avoid.Any(
					kv =>
					{
						return Vector3.Distance(kv.Key.Transform.Position.Value.NewY(0f), model.Transform.Position.Value.NewY(0f)) < kv.Value;
					}
				);
			}

			foreach (var model in payload.Game.Flora.AllActive.Where(hasCollision)) model.PooledState.Value = PooledStates.InActive;
			foreach (var model in payload.Game.Debris.AllActive.Where(hasCollision)) model.PooledState.Value = PooledStates.InActive;
			
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
			payload.Game.GenerationLog.Append(GenerationEvents.End);

			var total = payload.Game.GenerationLog.TotalTime;
			var roomTotal = payload.Game.GenerationLog.GetTimeBetween(
				GenerationEvents.RoomGenerationBegin,
				GenerationEvents.RoomGenerationEnd
			);
			var floraTotal = payload.Game.GenerationLog.GetTimeBetween(
				GenerationEvents.FloraSeedAppend,
				GenerationEvents.FloraClusterAppend
			);

			var result = "Generated " + payload.Game.Rooms.AllActive.Length + " rooms in " + total.TotalSeconds.ToString("N2") + " seconds for seed " + generator.Seed;

			result += "\n - Elapsed Generation Time -";
			result += "\n   - Room: " + roomTotal.TotalSeconds.ToString("N2");
			result += "\n   - Flora: " + floraTotal.TotalSeconds.ToString("N2");
			
			Debug.Log(result);
		}
		
		#region Shared Events
		void OnCalculateNavigation(Action done)
		{
			payload.Game.GenerationLog.Append(GenerationEvents.CalculateNavigationBegin);
			payload.Game.NavigationMesh.QueueCalculation();

			App.Heartbeat.WaitForCondition(
				() =>
				{
					payload.Game.GenerationLog.Append(GenerationEvents.CalculateNavigationEnd);
					done();	
				},
				() => payload.Game.NavigationMesh.CalculationState.Value == NavigationMeshModel.CalculationStates.Completed
			);
		}
		#endregion
		
		#region Utility
		void GenerateDebrisInRoom(
			RoomModel room
		)
		{
			foreach (var debrisId in payload.Game.Debris.ValidPrefabIds)
			{
				TryGenerating(
					room,
					4,
					position =>
					{
						// TODO: Calculate collisions with other debris...
						payload.Game.Debris.Activate(
							debrisId,
							room.RoomTransform.Id.Value,
							position,
							generator.GetNextRotation()
						);

						return true;
					}
				);
			}
			
		}
		
		void GenerateFloraInRoom(
			RoomModel room,
			FloraPoolModel.SpeciesData[] species
		)
		{
			if (species.None()) return;
			
			var parentPool = new List<FloraModel>();

			foreach (var currentSpecies in species)
			{
				var currentCount = 0;

				if (currentSpecies.CountPerRoomMinimum == currentSpecies.CountPerRoomMaximum) currentCount = currentSpecies.CountPerRoomMinimum;
				else currentCount = generator.GetNextInteger(currentSpecies.CountPerRoomMinimum, currentSpecies.CountPerRoomMaximum + 1);

				if (room.IsSpawn.Value && currentSpecies.RequiredInSpawn) currentCount = Mathf.Max(currentCount, 1);
				
				if (currentCount == 0) continue;
				
				TryGenerating(
					room,
					currentCount,
					position =>
					{
						foreach (var possibleCollision in payload.Game.Flora.AllActive)
						{
							if (possibleCollision.RoomTransform.Id.Value != room.RoomTransform.Id.Value) continue;
							if (possibleCollision.ReproductionRadius.Value.Maximum < Vector3.Distance(possibleCollision.Transform.Position.Value, position)) continue;

							return false;
						}

						parentPool.Add(
							payload.Game.Flora.ActivateAdult(
								currentSpecies.Species,
								room.RoomTransform.Id.Value,
								position
							)
						);
						
						payload.Game.GenerationLog.Append(GenerationEvents.FloraSeedAppend);
						return true;
					}
				);
			}

			foreach (var currentSpecies in species)
			{
				int clusterCount;

				if (currentSpecies.CountPerClusterMinimum == currentSpecies.CountPerClusterMaximum) clusterCount = currentSpecies.CountPerClusterMinimum;
				else clusterCount = generator.GetNextInteger(currentSpecies.CountPerClusterMinimum, currentSpecies.CountPerClusterMaximum + 1);
				
				if (clusterCount <= 1) continue;

				clusterCount = currentSpecies.CountPerClusterMaximum;
				
				var currentSpeciesParentPool = parentPool
					.Where(m => m.Species.Value == currentSpecies.Species)
					.ToList();
				
				if (currentSpeciesParentPool.None()) continue;
				
				TryGenerating(
					room,
					clusterCount - 1,
					position =>
					{
						var offspring = generator.GetNextFrom(currentSpeciesParentPool).TriggerReproduction(generator);

						if (offspring == null) return false;
						
						currentSpeciesParentPool.Add(offspring);
						payload.Game.GenerationLog.Append(GenerationEvents.FloraClusterAppend);
						
						return true;
					}
				);
			}
		}

		void TryGenerating(
			RoomModel room,
			int count,
			Func<Vector3, bool> generate
		)
		{
			var budgetRemaining = count;
			var failureBudgetRemaining = budgetRemaining * 2; // TODO: Don't hardcode this...

			while (0 < budgetRemaining && 0 < failureBudgetRemaining)
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

				if (generate(hit.position)) budgetRemaining--;
				else failureBudgetRemaining--;
			}
		}
		#endregion
	}
}