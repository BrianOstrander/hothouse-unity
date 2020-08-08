using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Ai;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
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
		}

		public void Push()
		{
			App.S.PushWaitForFixedUpdate();
			
			App.S.Push(OnBegin);

			App.S.PushBlocking(OnGenerateRooms);
			App.S.PushBlocking(OnGenerateSpawn);
			
			App.S.PushWaitForFixedUpdate();
			
			App.S.PushBlocking(OnGenerateWallDecorations);
			
			App.S.PushBlocking(OnCalculateNavigation);
			
			App.S.PushBlocking(OnGenerateGenerators);
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
			
			App.S.Push(() => payload.Game.SimulationMultiplier.Value = 1f);
			App.S.Push(OnEnd);
		}

		void OnBegin()
		{
			generator = new Demon(payload.Game.LevelGeneration.Seed.Value);
			
			request = RoomResolverRequest.Defaults.Tiny(
				generator,
				payload.Game.Rooms.Activate,
				payload.Game.Doors.Activate
			);
			
			payload.Game.LevelGeneration.Log.Append(GenerationEvents.Begin);
			payload.Game.SimulationMultiplier.Value = 0f;
			payload.Game.ResetSimulationInitialized();
		}

		void OnGenerateRooms(Action done)
		{
			payload.Game.LevelGeneration.Log.Append(GenerationEvents.RoomGenerationBegin);
			payload.Game.RoomResolver.Generate(
				request,
				result =>
				{
					foreach (var room in payload.Game.Rooms.AllActive) room.IsRevealed.Value = true;
					payload.Game.LevelGeneration.Log.Append(GenerationEvents.RoomGenerationEnd);
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
			
			payload.Game.LevelGeneration.Log.Append(GenerationEvents.SpawnChosen);
			done();
		}

		void OnGenerateWallDecorations(Action done)
		{
			var wallDecorations = App.V.GetPrefabs<DecorationView>()
				.Where(v => v.View.PrefabTags.Contains(DecorationView.Constants.Tags.DecorationWall))
				.Select(v => v.View)
				.ToArray();

			var minimumWallDecorationWidth = wallDecorations
				.OrderBy(v => v.ExtentsLeftRightWidth)
				.Select(v => v.ExtentsLeftRightWidth)
				.Min();
			
			var minimumWallDecorationHeight = wallDecorations
				.OrderBy(v => v.ExtentHeight)
				.Select(v => v.ExtentHeight)
				.Min();
			
			var roomInfo = new DecorationRule.RoomInfo();
			
			foreach (var room in payload.Game.Rooms.AllActive)
			{
				roomInfo.ResetForRoom(room);

				if (room.IsSpawn.Value)
				{
					roomInfo.DecorationTagsRequiredForRoom.Add(DecorationView.Constants.Tags.Sources.Water, 1);
					
					roomInfo.DecorationTagsBudgetForRoom.Add(DecorationView.Constants.Tags.Sources.Water, 1);
				}
				else
				{
					roomInfo.DecorationTagsBudgetForRoom.Add(DecorationView.Constants.Tags.Sources.Water, 2);
				}
				
				var remainingWalls = room.Walls.Value
					.Where(w => w.Valid).ToList();

				var decorationBudgetRemaining = generator.GetNextInteger(
					Mathf.Max(request.RoomWallDecorationsMinimum, roomInfo.DecorationTagsRequiredForRoom.Sum(kv => kv.Value)),
					request.RoomWallDecorationsMaximum
				);

				while (remainingWalls.Any() && 0 < decorationBudgetRemaining)
				{
					var wallIndex = generator.GetNextInteger(0, remainingWalls.Count);
					var wall = remainingWalls[wallIndex];
					remainingWalls.RemoveAt(wallIndex);

					var wallWidth = Vector3.Distance(wall.Begin, wall.End);
					
					roomInfo.ResetForWall(
						wallWidth,
						wall.Height
					);
					
					var validDecorations = payload.Game.Decorations.GetValidViews(
						wallDecorations,
						roomInfo
					);

					if (validDecorations.None()) continue;

					var clutter = 1f;

					if (decorationBudgetRemaining < request.RoomWallDecorationsMinimum)
					{
						clutter = Mathf.Max(request.RoomClutterMinimum, generator.NextFloat);
					}

					var segmentNormal = (wall.End - wall.Begin).normalized;
					
					var segmentWidthRemaining = wallWidth;
					var segmentBegin = wall.Begin;

					while (minimumWallDecorationWidth < segmentWidthRemaining)
					{
						var segmentDivision = generator.NextFloat;
						var segmentWidthMaximum = segmentWidthRemaining - Mathf.Max(1f - segmentDivision, segmentDivision);
						var segmentWidthMinimum = segmentWidthRemaining - segmentWidthMaximum;
						
						var segmentBeginMaximum = segmentBegin;
						var segmentEndMaximum = segmentBegin + (segmentNormal * segmentWidthMaximum);

						var segmentBeginMinimum = segmentEndMaximum;
						// var segmentEndMinimum = segmentBeginMinimum + (segmentNormal * segmentWidthMinimum);

						float segmentWidthChosen;
						Vector3 segmentBeginChosen;
						float segmentWidthNotChosen;
						Vector3 segmentBeginNotChosen;

						if (generator.NextBool)
						{
							segmentWidthChosen = segmentWidthMinimum;
							segmentBeginChosen = segmentBeginMinimum;
							segmentWidthNotChosen = segmentWidthMaximum;
							segmentBeginNotChosen = segmentBeginMaximum;
						}
						else
						{
							segmentWidthChosen = segmentWidthMaximum;
							segmentBeginChosen = segmentBeginMaximum;
							segmentWidthNotChosen = segmentWidthMinimum;
							segmentBeginNotChosen = segmentBeginMinimum;
						}

						roomInfo.WallSegmentWidth = segmentWidthChosen;
						
						validDecorations = payload.Game.Decorations.GetValidViews(
							validDecorations,
							roomInfo
						);

						if (validDecorations.Any() && generator.NextFloat < clutter)
						{
							// var offset = generator.GetNextFloat(0f, 1f);
							// var color = Color.red.NewH(generator.NextFloat);
							// var segmentEndChosen = segmentBeginChosen + (segmentNormal * segmentWidthChosen);
							// Debug.DrawLine(
							// 	segmentBeginChosen + (Vector3.up * offset),
							// 	segmentEndChosen + (Vector3.up * offset),
							// 	color,
							// 	999f
							// );
							//
							// Debug.DrawLine(
							// 	segmentBeginChosen,
							// 	segmentBeginChosen + (Vector3.up * offset),
							// 	color,
							// 	999f
							// );
							//
							// Debug.DrawLine(
							// 	segmentEndChosen,
							// 	segmentEndChosen + (Vector3.up * offset),
							// 	color,
							// 	999f
							// );

							var decorationPosition = segmentBeginChosen + (segmentNormal * (segmentWidthChosen * 0.5f));

							var didHit = Physics.Raycast(
								decorationPosition + (Vector3.up * (RoomView.Constants.Walls.HeightIncrements)),
								-wall.Normal,
								out var wallHit,
								RoomView.Constants.Walls.DistanceMaximum * 2f,
								LayerMasks.DefaultAndFloor
							);

							if (didHit)
							{
								decorationBudgetRemaining--;

								var validDecorationsRequired = payload.Game.Decorations.GetValidViewsRequired(
									validDecorations,
									roomInfo
								);

								payload.Game.Decorations.Activate(
									generator.GetNextFrom(validDecorationsRequired.Any() ? validDecorationsRequired : validDecorations).PrefabId,
									roomInfo,
									wallHit.point.NewY(decorationPosition.y),
									Quaternion.LookRotation(wall.Normal)
								);
							}
							else Debug.LogWarning("Missed hitting a wall, unlikely but not impossible...");
						}

						segmentWidthRemaining = segmentWidthNotChosen;
						segmentBegin = segmentBeginNotChosen;
					}
				}	
			}
			
			done();
		}

		void OnGenerateGenerators(Action done)
		{
			bool getDecorationEntrance(
				DecorationModel model,
				out Vector3 position
			)
			{
				position = Vector3.zero;
				
				var isNavigable = NavigationUtility.CalculateNearestFloor(
					model.PossibleEntrance.Value,
					out var navHit,
					out var physicsHit,
					out var roomId
				);
				
				if (!isNavigable) return false;
				if (!model.IsInRoom(roomId)) return false;
				
				position = navHit.position;
				
				return true;
			}
			
			foreach (var room in payload.Game.Rooms.AllActive)
			{
				foreach (var decoration in payload.Game.Decorations.AllActive.Where(m => room.RoomContains(m)))
				{
					foreach (var tag in decoration.PrefabTags.Value)
					{
						switch (tag)
						{
							case DecorationView.Constants.Tags.Sources.Water:
								if (getDecorationEntrance(decoration, out var position))
								{
									payload.Game.Generators.Activate(
										decoration,
										position,
										decoration.Transform.Rotation.Value,
										new FloatRange(8f, 24f),
										new FloatRange(8f, 12f),
										(Inventory.Types.Water, 1, 2)
									);
								}
								else
								{
									Debug.DrawRay(
										decoration.PossibleEntrance.Value,
										Vector3.up,
										Color.red,
										999f
									);
									Debug.LogError("Unable to get a valid entrance for Decoration with id: " + decoration.Id.Value);
								}
								break;
							default:
								if (tag.StartsWith(DecorationView.Constants.Tags.Sources.Prefix))
								{
									Debug.LogError($"Tag \"{tag}\" begins with \"{DecorationView.Constants.Tags.Sources.Prefix}\" but was not handled");
								}
								break;
						}
					}
				}
			}
			
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
					payload.Game.Flora.GetTypesValidForRoom(room)	
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
				Jobs.Laborer,
				Jobs.Laborer,
				Jobs.Laborer,
				Jobs.Laborer,
				Jobs.Laborer
			};

			for (var i = 0; i < requiredJobs.Length; i++)
			{
				var position = spawn.Transform.Position.Value + (Vector3.forward * (4f + i));

				if (Physics.Raycast(new Ray(position, Vector3.down), out var hit, 40f, LayerMasks.Floor))
				{
					position = hit.point;
				}
				
				var dweller = payload.Game.Dwellers.Activate(
					spawn.Id.Value,
					position,
					generator
				);

				if (i < requiredJobs.Length) dweller.Job.Value = requiredJobs[i];
				else dweller.Job.Value = generator.GetNextFrom(randomJobPool);
				
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
			
			// var wagon = payload.Game.Buildings.Activate<StartingWagonDefinition>(
			// 	spawn.Id.Value,
			// 	position + (Vector3.left * 2f),
			// 	Quaternion.identity * Quaternion.Euler(0f, 90f, 0f),
			// 	BuildingStates.Operating
			// );
			//
			// var startingResources = new[]
			// {
			// 	Inventory.Types.StalkSeed
			// };
			//
			// wagon.Inventory.Add(
			// 	Inventory.FromEntries(
			// 		wagon.Inventory.AllCapacity.Value.GetMaximum().Entries
			// 			.Where(e => startingResources.Contains(e.Type))
			// 			.ToArray()
			// 	)
			// );
		
			var bonfire = payload.Game.Buildings.Activate<BonfireLightDefinition>(
				spawn.Id.Value,
				position + (Vector3.right * 6f),
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
			// payload.Game.DesireDamageMultiplier.Value = 0f;
			// payload.Game.SimulationMultiplier.Value = 60f;
			//
			// payload.Game.Buildings.Activate<BedrollDefinition>(
			// 	spawn.RoomTransform.Id.Value,
			// 	position + (Vector3.right * 2f) + (Vector3.back * -3f),
			// 	Quaternion.identity,
			// 	BuildingStates.Operating
			// );
			//
			// payload.Game.Buildings.Activate<BedrollDefinition>(
			// 	spawn.RoomTransform.Id.Value,
			// 	position + (Vector3.right * 4f) + (Vector3.back * -3f),
			// 	Quaternion.identity,
			// 	BuildingStates.Operating
			// );
			//
			// payload.Game.Buildings.Activate<BedrollDefinition>(
			// 	spawn.RoomTransform.Id.Value,
			// 	position + (Vector3.right * 6f) + (Vector3.back * -3f),
			// 	Quaternion.identity,
			// 	BuildingStates.Operating
			// );
			//
			// payload.Game.Buildings.Activate<BedrollDefinition>(
			// 	spawn.RoomTransform.Id.Value,
			// 	position + (Vector3.right * 8f) + (Vector3.back * -3f),
			// 	Quaternion.identity,
			// 	BuildingStates.Operating
			// );
			//
			// var farm = payload.Game.Buildings.Activate<StalkSeedSiloDefinition>(
			// 	spawn.RoomTransform.Id.Value,
			// 	position + (Vector3.right * 4f) + (Vector3.back * 3f),
			// 	Quaternion.identity,
			// 	BuildingStates.Operating
			// );
			//
			// farm.Inventory.Add(farm.Inventory.AvailableCapacity.Value.GetMaximum());
			//
			// var smokerack = payload.Game.Buildings.Activate<SmokeRackDefinition>(
			// 	spawn.RoomTransform.Id.Value,
			// 	position + (Vector3.right * -4f) + (Vector3.back * 3f),
			// 	Quaternion.identity,
			// 	BuildingStates.Operating
			// );
			//
			// smokerack.Recipes.Queue.Value = smokerack.Recipes.Queue.Value
			// 	.Append(RecipeComponent.RecipeIteration.ForDesired(smokerack.Recipes.Available.Value.Last(), 20))
			// 	.ToArray();
			//
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
			payload.Game.LevelGeneration.Log.Append(GenerationEvents.End);

			var total = payload.Game.LevelGeneration.Log.TotalTime;
			var roomTotal = payload.Game.LevelGeneration.Log.GetTimeBetween(
				GenerationEvents.RoomGenerationBegin,
				GenerationEvents.RoomGenerationEnd
			);
			var floraTotal = payload.Game.LevelGeneration.Log.GetTimeBetween(
				GenerationEvents.FloraSeedAppend,
				GenerationEvents.FloraClusterAppend
			);

			var result = "Generated " + payload.Game.Rooms.AllActive.Length + " rooms in " + total.TotalSeconds.ToString("N2") + " seconds for seed " + generator.Seed;

			result += "\n - Elapsed Generation Time -";
			result += "\n   - Room: " + roomTotal.TotalSeconds.ToString("N2");
			result += "\n   - Flora: " + floraTotal.TotalSeconds.ToString("N2");
			
			// Debug.Log(result);
		}
		
		#region Shared Events
		void OnCalculateNavigation(Action done)
		{
			payload.Game.LevelGeneration.Log.Append(GenerationEvents.CalculateNavigationBegin);
			payload.Game.NavigationMesh.QueueCalculation();

			App.Heartbeat.WaitForCondition(
				() =>
				{
					payload.Game.LevelGeneration.Log.Append(GenerationEvents.CalculateNavigationEnd);
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
			FloraDefinition[] species
		)
		{
			if (species.None()) return;
			
			var parentPool = new List<FloraModel>();

			foreach (var currentSpecies in species)
			{
				var currentCount = 0;

				if (currentSpecies.ClusterPerRoom.Minimum == currentSpecies.ClusterPerRoom.Maximum) currentCount = currentSpecies.ClusterPerRoom.Minimum;
				else currentCount = generator.GetNextInteger(currentSpecies.ClusterPerRoom.Minimum, currentSpecies.ClusterPerRoom.Maximum + 1);

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
							payload.Game.Flora.Activate(
								currentSpecies,
								room.RoomTransform.Id.Value,
								position,
								isAdult: true,
								generator: generator
							)
						);
						
						payload.Game.LevelGeneration.Log.Append(GenerationEvents.FloraSeedAppend);
						return true;
					}
				);
			}

			while (parentPool.Any())
			{
				var nextIndex = generator.GetNextInteger(0, parentPool.Count);
				var next = parentPool[nextIndex];
				parentPool.RemoveAt(nextIndex);

				var currentSpecies = species.First(d => d.Type == next.Type.Value);
				
				int clusterRemaining;

				if (currentSpecies.CountPerCluster.Minimum == currentSpecies.CountPerCluster.Maximum) clusterRemaining = currentSpecies.CountPerCluster.Minimum;
				else clusterRemaining = generator.GetNextInteger(currentSpecies.CountPerCluster.Minimum, currentSpecies.CountPerCluster.Maximum + 1);

				if (clusterRemaining <= 0) continue;

				var clusterBoundaryParent = clusterRemaining / 2;
				
				var clusterFailureBudget = clusterRemaining * 2;

				var clusterElements = next.WrapInList();
				
				while (0 < clusterRemaining && 0 < clusterFailureBudget)
				{
					var offspring = clusterElements[generator.GetNextInteger(0, Mathf.Max(1, Mathf.Min(clusterElements.Count, clusterBoundaryParent)))].TriggerReproduction(generator);

					if (offspring == null)
					{
						clusterFailureBudget--;
						continue;
					}
					
					clusterElements.Add(offspring);
					clusterRemaining--;
					payload.Game.LevelGeneration.Log.Append(GenerationEvents.FloraClusterAppend);
				}
			}
		}

		int TryGenerating(
			RoomModel room,
			int count,
			Func<Vector3, bool> generate
		)
		{
			return TryGenerating(
				room,
				count,
				count * 2,
				generate
			);
		}

		int TryGenerating(
			RoomModel room,
			int count,
			int failureLimit,
			Func<Vector3, bool> generate
		)
		{
			var budgetRemaining = count;
			var failureBudgetRemaining = Mathf.Max(count, failureLimit); // TODO: Don't hardcode this...

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

			return count - budgetRemaining;
		}
		#endregion
	}
}