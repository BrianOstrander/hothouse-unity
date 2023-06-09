using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.NumberDemon;
using Lunra.StyxMvp;
using UnityEngine;
using UnityEngine.Serialization;

namespace Lunra.Hothouse.Views
{
	public class RoomResolverView : View
	{
		struct WorkspaceCache
		{
			public RoomResolverRequest Request;
			public Action<RoomResolverResult> Done;
			public RoomResolverResult Result;

			public List<CollisionResolverDefinitionLeaf> Rooms;
			public List<CollisionResolverDefinitionLeaf> Doors;
			
			public List<CollisionResolverDefinitionLeaf> AvailableDoors;

			public int RoomCount => Rooms.Count;
			public Demon Generator => Request.Generator;
			
			public WorkspaceCache(
				RoomResolverRequest request,
				Action<RoomResolverResult> done
			)
			{
				Request = request;
				Done = done;
				Result = new RoomResolverResult();

				Result.Request = request;
				
				Rooms = new List<CollisionResolverDefinitionLeaf>();
				Doors = new List<CollisionResolverDefinitionLeaf>();

				AvailableDoors = new List<CollisionResolverDefinitionLeaf>();
			}
		}
		
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null

		[SerializeField] CollisionResolverDefinitionLeaf definitionPrefab;
		[SerializeField] Transform roomPrefabsRoot;
		[SerializeField] Transform doorPrefabsRoot;
		[SerializeField] Transform workspaceRoot;
		
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion
		
		#region Bindings
		public int RoomGenerationTimeouts => 5;

		public void AddRoomDefinition(
			string prefabId,
			ColliderCache[] roomColliders,
			DoorCache[] doorAnchors,
			WallCache[] walls,
			string[] prefabTags
		)
		{
			roomDefinitions.Add(
				InstantiateDefinition(
					roomPrefabsRoot,
					CollisionResolverDefinitionLeaf.Types.Room,
					prefabId,
					roomColliders,
					doorAnchors,
					walls,
					prefabTags
				)	
			);
		}
		
		public void AddDoorDefinition(
			string prefabId,
			DoorCache[] doorAnchors,
			string[] prefabTags
		)
		{
			doorDefinitions.Add(
				InstantiateDefinition(
					doorPrefabsRoot,
					CollisionResolverDefinitionLeaf.Types.Door,
					prefabId,
					null,
					doorAnchors,
					null,
					prefabTags
				)	
			);
		}

		CollisionResolverDefinitionLeaf InstantiateDefinition(
			Transform root,
			CollisionResolverDefinitionLeaf.Types type,
			string prefabId,
			ColliderCache[] roomColliders,
			DoorCache[] doorAnchors,
			WallCache[] walls,
			string[] prefabTags
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
				doorAnchors,
				walls,
				prefabTags
			);

			return definition;
		}
		#endregion

		#region Local
		List<CollisionResolverDefinitionLeaf> roomDefinitions = new List<CollisionResolverDefinitionLeaf>();
		List<CollisionResolverDefinitionLeaf> doorDefinitions = new List<CollisionResolverDefinitionLeaf>();

		WorkspaceCache workspaceCache;
		#endregion
		
		public override void Cleanup()
		{
			base.Cleanup();
			
			definitionPrefab.gameObject.SetActive(false);
			roomPrefabsRoot.gameObject.SetActive(false);
			doorPrefabsRoot.gameObject.SetActive(false);

			foreach (var definition in roomDefinitions)
			{
				Destroy(definition.gameObject);
			}
			
			roomDefinitions.Clear();
		}

		CollisionResolverDefinitionLeaf WorkspaceInstantiate(
			List<CollisionResolverDefinitionLeaf> pool,
			Func<CollisionResolverDefinitionLeaf, bool> predicate = null,
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
			Debug.LogWarning("Disabled");
			// Generate(
			// 	RoomResolverRequest.Default(
			// 		DemonUtility.GetNextInteger(int.MinValue, int.MaxValue),
			// 		null,
			// 		null,
			// 		result =>
			// 		{
			// 			Debug.Log(result);
			// 			App.Heartbeat.WaitForSeconds(ReGenerate, 0.1f);
			// 		}
			// 	)
			// );
		}
		
		public void Generate(
			RoomResolverRequest request,
			Action<RoomResolverResult> done
		)
		{
			workspaceRoot.ClearChildren();

			workspaceCache = new WorkspaceCache(
				request,
				done
			);
			
			StartCoroutine(OnGenerate());
		}

		IEnumerator OnGenerate()
		{
			yield return new WaitForFixedUpdate();
			
			var root = WorkspaceInstantiate(
				roomDefinitions,
				r => r.PrefabTags.Contains(PrefabTagCategories.Room.Spawn)
			);

			if (root == null)
			{
				Debug.LogError("root is null, this should never happen");
				// TODO: Handle this error
			}

			workspaceCache.Rooms.Add(root);
			workspaceCache.AvailableDoors.AddRange(OnGenerateAppendDoors(root));

			while (workspaceCache.AvailableDoors.Any() && workspaceCache.RoomCount < workspaceCache.Request.RoomCountTarget)
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

				model.Tags.AddTags(instance.PrefabTags, DayTime.MaxValue, fromPrefab: true);

				var walls = new WallCache[instance.Walls.Length];

				for (var i = 0; i < walls.Length; i++)
				{
					var wall = new WallCache();
					var wallInstance = instance.Walls[i];
					
					wall.Index = wallInstance.Index;
					
					if (wallInstance.DoorIndex == -1) wall.DoorIndex = null;
					else wall.DoorIndex = wallInstance.DoorIndex;

					wall.Begin = wallInstance.BeginAnchor.position;
					wall.End = wallInstance.EndAnchor.position;
					wall.Normal = wallInstance.BeginAnchor.forward;
					wall.Height = wallInstance.Height;

					walls[i] = wall;
				}

				model.Walls.Value = walls;
				
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
				
				model.Tags.AddTags(instance.PrefabTags, DayTime.MaxValue, fromPrefab: true);

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

					try
					{
						var roomReference = workspaceCache.Rooms.First(m => m.Id == room.RoomTransform.Id.Value);
						var closestIndex = roomReference.DoorAnchors
							.OrderBy(
								d => Mathf.Min(
									Vector3.Distance(d.Anchor.position, instance.DoorAnchors.First().Anchor.position),
									Vector3.Distance(d.Anchor.position, instance.DoorAnchors.Last().Anchor.position)
								)
							)
							.First();

						room.UnPluggedDoors.Value = room.UnPluggedDoors.Value.Append(closestIndex.Index).ToArray();
					}
					catch (Exception e) { Debug.LogException(e); }
				}
				
				doors.Add(model);
			}

			foreach (var room in rooms)
			{
				var walls = room.Walls.Value;
				for (var i = 0; i < walls.Length; i++)
				{
					walls[i].Valid = !walls[i].DoorIndex.HasValue || !room.UnPluggedDoors.Value.Contains(walls[i].DoorIndex.Value);
				}
				room.Walls.Value = walls;
			}

			workspaceCache.Result.Rooms = rooms.ToArray();
			workspaceCache.Result.Doors = doors.ToArray();

			yield return null;

			OnGenerateDone();
		}

		IEnumerator OnGenerateRoom(CollisionResolverDefinitionLeaf originDoor)
		{
			var generationSuccess = false;

			var invalidRoomIds = new List<string>();
			
			for (var i = 0; i < RoomGenerationTimeouts; i++)
			{
				var possibleRoom = WorkspaceInstantiate(
					roomDefinitions,
					r =>
					{
						if (invalidRoomIds.Contains(r.PrefabId)) return false;
						return r.DoorAnchors.Any(d => d.Size == originDoor.DoorAnchorSizeMaximum);
					},
					true
				);

				if (possibleRoom == null) break;
				
				invalidRoomIds.Add(possibleRoom.PrefabId);

				// yield return new WaitForFixedUpdate();

				for (var doorIndex = 0; doorIndex < possibleRoom.DoorAnchors.Length; doorIndex++)
				{
					var door = possibleRoom.DoorAnchors[doorIndex];

					if (door.Size != originDoor.DoorAnchorSizeMaximum) continue;
					
					possibleRoom.Dock(
						door,
						originDoor.DoorAnchors.Last()
					);
					
					// Investigate speeding up map generation by using translate instead of waiting for a fixed update.
					// door.Anchor.Translate(Vector3.zero);
					yield return new WaitForFixedUpdate();

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

		CollisionResolverDefinitionLeaf[] OnGenerateAppendDoors(CollisionResolverDefinitionLeaf room)
		{
			var results = new List<CollisionResolverDefinitionLeaf>();
			
			for (var i = 0; i < room.DoorAnchors.Length; i++)
			{
				var roomDoorOpening = room.DoorAnchors[i];
				
				if (!string.IsNullOrEmpty(roomDoorOpening.Id)) continue;

				var door = WorkspaceInstantiate(
					doorDefinitions,
					d => roomDoorOpening.Size == d.DoorAnchorSizeMaximum
				);

				if (door == null)
				{
					Debug.LogError("Unable to find a door that is " + roomDoorOpening.Size + " meters wide");
					continue;
				}

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
			workspaceCache.Done(workspaceCache.Result);
		}
	}
}