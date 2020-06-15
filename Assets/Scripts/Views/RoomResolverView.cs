using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.NumberDemon;
using Lunra.StyxMvp;
using UnityEngine;

namespace Lunra.Hothouse.Views
{
	public class RoomResolverView : View
	{
		struct WorkspaceCache
		{
			public RoomResolverRequest Request;
			public RoomResolverResult Result;

			public DateTime BeginTime;
			public Demon Generator;
			
			public int RoomCountTarget;

			public List<CollisionResolverDefinition> Rooms;
			public List<CollisionResolverDefinition> Doors;
			
			public List<CollisionResolverDefinition> AvailableDoors;

			public int RoomCount => Rooms.Count;
			
			public WorkspaceCache(RoomResolverRequest request)
			{
				Request = request;
				Result = new RoomResolverResult();

				Result.Request = request;
				
				BeginTime = DateTime.Now;

				Generator = new Demon(request.Seed);
				
				RoomCountTarget = Generator.GetNextInteger(request.RoomCountMinimum, request.RoomCountMaximum);
				
				Rooms = new List<CollisionResolverDefinition>();
				Doors = new List<CollisionResolverDefinition>();

				AvailableDoors = new List<CollisionResolverDefinition>();
			}
		}
		
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null

		[SerializeField] CollisionResolverDefinition definitionPrefab;
		[SerializeField] Transform roomPrefabsRoot;
		[SerializeField] Transform doorPrefabsRoot;
		[SerializeField] Transform workspaceRoot;
		
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion
		
		#region Bindings
		public int RoomGenerationTimeouts => 5;

		public void AddRoomDefinition(
			string prefabId,
			RoomCollider[] roomColliders,
			(Vector3 Position, Vector3 Forward)[] doorAnchors
		)
		{
			roomDefinitions.Add(
				InstantiateDefinition(
					roomPrefabsRoot,
					CollisionResolverDefinition.Types.Room,
					prefabId,
					roomColliders,
					doorAnchors
				)	
			);
		}
		
		public void AddDoorDefinition(
			string prefabId,
			(Vector3 Position, Vector3 Forward)[] doorAnchors
		)
		{
			doorDefinitions.Add(
				InstantiateDefinition(
					doorPrefabsRoot,
					CollisionResolverDefinition.Types.Door,
					prefabId,
					null,
					doorAnchors
				)	
			);
		}

		CollisionResolverDefinition InstantiateDefinition(
			Transform root,
			CollisionResolverDefinition.Types type,
			string prefabId,
			RoomCollider[] roomColliders,
			(Vector3 Position, Vector3 Forward)[] doorAnchors
		)
		{
			var definition = RootGameObject.InstantiateChild(
				definitionPrefab,
				setActive: true
			);
			
			definition.transform.SetParent(root);

			definition.name = prefabId;

			definition.Define(
				type,
				prefabId,
				roomColliders,
				doorAnchors
			);

			return definition;
		}
		#endregion

		#region Local
		List<CollisionResolverDefinition> roomDefinitions = new List<CollisionResolverDefinition>();
		List<CollisionResolverDefinition> doorDefinitions = new List<CollisionResolverDefinition>();

		WorkspaceCache workspaceCache;
		#endregion
		
		public override void Reset()
		{
			base.Reset();
			
			definitionPrefab.gameObject.SetActive(false);
			roomPrefabsRoot.gameObject.SetActive(false);
			doorPrefabsRoot.gameObject.SetActive(false);

			foreach (var definition in roomDefinitions)
			{
				Destroy(definition.gameObject);
			}
			
			roomDefinitions.Clear();
		}

		CollisionResolverDefinition WorkspaceInstantiate(
			List<CollisionResolverDefinition> pool,
			Func<CollisionResolverDefinition, bool> predicate = null,
			bool zeroSiblingIndex = false
		)
		{
			var prefab = predicate == null ? workspaceCache.Generator.GetNextFrom(pool) : workspaceCache.Generator.GetNextFrom(pool.Where(predicate));
			
			if (prefab == null) return null;
			
			var result = workspaceRoot.gameObject.InstantiateChild(prefab);
			result.Id = Guid.NewGuid().ToString();

			if (zeroSiblingIndex) result.transform.SetSiblingIndex(0);
			
			return result;
		}

		[ContextMenu("ReGenerate")]
		void ReGenerate()
		{
			// Debug.LogWarning("Disabled");
			Generate(
				RoomResolverRequest.Default(
					DemonUtility.GetNextInteger(int.MinValue, int.MaxValue),
					null,
					null,
					result =>
					{
						Debug.Log(result);
						App.Heartbeat.Wait(ReGenerate, 0.1f);
					}
				)
			);
		}
		
		public void Generate(RoomResolverRequest request)
		{
			workspaceRoot.ClearChildren();

			workspaceCache = new WorkspaceCache(request);
			
			StartCoroutine(OnGenerate());
		}

		IEnumerator OnGenerate()
		{
			yield return new WaitForFixedUpdate();
			
			var root = WorkspaceInstantiate(
				roomDefinitions,
				r => 4 <= r.DoorAnchors.Length
			);

			if (root == null)
			{
				Debug.LogError("root is null, this should never happen");
				// TODO: Handle this error
			}

			workspaceCache.Rooms.Add(root);
			workspaceCache.AvailableDoors.AddRange(OnGenerateAppendDoors(root));

			while (workspaceCache.AvailableDoors.Any() && workspaceCache.RoomCount < workspaceCache.RoomCountTarget)
			{
				var door = workspaceCache.Generator.GetNextFrom(workspaceCache.AvailableDoors);
				yield return StartCoroutine(OnGenerateRoom(door));
			}

			workspaceCache.Doors.RemoveAll(d => string.IsNullOrEmpty(d.DoorAnchors.Last().Id));

			yield return OnGeneratePopulateResult();
		}

		IEnumerator OnGeneratePopulateResult()
		{
			var rooms = new List<RoomModel>();
			var doors = new List<DoorModel>();

			foreach (var instance in workspaceCache.Rooms)
			{
				var model = workspaceCache.Request.ActivateRoom(
					instance.Id,
					instance.PrefabId,
					instance.transform.position,
					instance.transform.rotation
				);
				
				rooms.Add(model);
			}
			
			foreach (var instance in workspaceCache.Doors)
			{
				var model = workspaceCache.Request.ActivateDoor(
					instance.Id,
					instance.PrefabId,
					instance.DoorAnchors.First().Id,
					instance.DoorAnchors.Last().Id,
					instance.transform.position,
					instance.transform.rotation
				);

				foreach (var room in rooms.Where(r => model.IsConnnecting(r.Id.Value)))
				{
					if (!model.GetConnection(room.Id.Value, out var toRoomId))
					{
						Debug.LogError("Invalid door connection: "+model);
						continue;
					}

					room.AdjacentRoomIds.Value = room.AdjacentRoomIds.Value.Append(new KeyValuePair<string, bool>(toRoomId, false)).ToReadonlyDictionary(
						kv => kv.Key,
						kv => kv.Value
					);
				}

				doors.Add(model);
			}

			workspaceCache.Result.Rooms = rooms.ToArray();
			workspaceCache.Result.Doors = doors.ToArray();

			yield return OnGenerateSpawn();
		}

		IEnumerator OnGenerateSpawn()
		{
			var spawnOptions = workspaceCache.Result.Rooms
				.Where(r => workspaceCache.Request.SpawnDoorCountRequired <= r.AdjacentRoomIds.Value.Count);
			
			var spawn = workspaceCache.Generator.GetNextFrom(spawnOptions);

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

				var neighboringRooms = workspaceCache.Result.Rooms.Where(r => next.AdjacentRoomIds.Value.Keys.Any(k => r.Id.Value == k));

				foreach (var neighboringRoom in neighboringRooms)
				{
					if (neighboringRoom.SpawnDistance.Value == int.MaxValue) remainingSpawnDistanceChecks.Add(neighboringRoom);
					neighboringRoom.SpawnDistance.Value = Mathf.Min(next.SpawnDistance.Value + 1, neighboringRoom.SpawnDistance.Value);
					neighboringRoom.RevealDistance.Value = neighboringRoom.SpawnDistance.Value;
				}

				spawnDistanceMaximum = Mathf.Max(spawnDistanceMaximum, next.SpawnDistance.Value);
			}

			var endOptions = workspaceCache.Result.Rooms
				.Where(r => Mathf.Min(spawnDistanceMaximum, workspaceCache.Request.ExitDistanceMinimum) <= r.SpawnDistance.Value);

			var exit = workspaceCache.Generator.GetNextFrom(endOptions);

			if (exit == null)
			{
				Debug.LogError("end is null, this should never happen");
				// TODO: Handle this error
			}

			exit.IsExit.Value = true; 
			
			yield return null;
			
			OnGenerateDone();
		}

		IEnumerator OnGenerateRoom(CollisionResolverDefinition originDoor)
		{
			var generationSuccess = false;

			var invalidRoomIds = new List<string>();
			
			for (var i = 0; i < RoomGenerationTimeouts; i++)
			{
				var possibleRoom = WorkspaceInstantiate(
					roomDefinitions,
					r => !invalidRoomIds.Contains(r.PrefabId),
					true
				);

				if (possibleRoom == null) break;
				
				invalidRoomIds.Add(possibleRoom.PrefabId);

				yield return new WaitForFixedUpdate();

				for (var doorIndex = 0; doorIndex < possibleRoom.DoorAnchors.Length; doorIndex++)
				{
					var door = possibleRoom.DoorAnchors[doorIndex];
					
					possibleRoom.Dock(
						door,
						originDoor.DoorAnchors.Last()
					);

					yield return new WaitForFixedUpdate();
					
					// for (var c = 0; c < possibleRoom.Colliders.Length * 2; c++)
					// {
					// 	yield return new WaitForFixedUpdate();
					// 	if (possibleRoom.HasCollisions()) break;
					// }

					if (!possibleRoom.HasCollisions())
					{
						door.Id = possibleRoom.Id;

						possibleRoom.DoorAnchors[doorIndex] = door;
						
						break;
					}
				}

				if (!possibleRoom.HasCollisions())
				{
					workspaceCache.AvailableDoors.Remove(originDoor);
					workspaceCache.Doors.Add(originDoor);
					
					originDoor.DoorAnchors[1].Id = possibleRoom.Id;

					workspaceCache.AvailableDoors.AddRange(
						OnGenerateAppendDoors(possibleRoom)	
					);

					yield return new WaitForFixedUpdate();
					
					workspaceCache.Rooms.Add(possibleRoom);

					generationSuccess = true;
					break;
				}

				Destroy(possibleRoom.gameObject);
				
				yield return new WaitForFixedUpdate();
			}

			if (!generationSuccess)
			{
				workspaceCache.AvailableDoors.Remove(originDoor);
				workspaceCache.Doors.Add(originDoor);
			}
		}

		CollisionResolverDefinition[] OnGenerateAppendDoors(CollisionResolverDefinition room)
		{
			var results = new List<CollisionResolverDefinition>();
			
			for (var i = 0; i < room.DoorAnchors.Length; i++)
			{
				var roomDoorOpening = room.DoorAnchors[i];
				
				if (!string.IsNullOrEmpty(roomDoorOpening.Id)) continue;

				var door = WorkspaceInstantiate(doorDefinitions);

				door.Dock(
					door.DoorAnchors.First(),
					roomDoorOpening
				);

				door.DoorAnchors[0].Id = room.Id;
				roomDoorOpening.Id = door.Id;

				room.DoorAnchors[i] = roomDoorOpening;
				
				results.Add(door);
			}

			return results.ToArray();
		}
		
		void OnGenerateDone()
		{
			workspaceCache.Result.GenerationElapsed = DateTime.Now - workspaceCache.BeginTime;
			
			workspaceCache.Request.Done(workspaceCache.Result);
		}
	}
}