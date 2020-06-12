using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.StyxMvp;
using UnityEngine;

namespace Lunra.Hothouse.Views
{
	public class RoomResolverView : View
	{
		struct GenerationData
		{
			public int RoomCountMaximum;

			public List<CollisionResolverDefinition> Rooms;
			public List<CollisionResolverDefinition> Doors;
			
			public List<CollisionResolverDefinition> AvailableDoors;

			public int RoomCount => Rooms.Count;
			
			public GenerationData(int roomCountMaximum)
			{
				RoomCountMaximum = roomCountMaximum;
				
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
		//
		// [SerializeField] string prefabId;
		// public string PrefabId => prefabId;
		//
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
					prefabId,
					null,
					doorAnchors
				)	
			);
		}

		CollisionResolverDefinition InstantiateDefinition(
			Transform root,
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
				prefabId,
				roomColliders,
				doorAnchors
			);

			return definition;
		}
		#endregion

		List<CollisionResolverDefinition> roomDefinitions = new List<CollisionResolverDefinition>();
		List<CollisionResolverDefinition> doorDefinitions = new List<CollisionResolverDefinition>();

		GenerationData generationData;
		
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
			var prefab = predicate == null ? pool.Random() : pool.Where(predicate).Random();
			
			if (prefab == null)
			{
				Debug.LogError("No valid definition found");
				return null;
			}

			var result = workspaceRoot.gameObject.InstantiateChild(prefab);
			result.Id = Guid.NewGuid().ToString();

			if (zeroSiblingIndex) result.transform.SetSiblingIndex(0);
			
			return result;
		}

		[ContextMenu("ReGenerate")]
		void ReGenerate()
		{
			Generate(() => Debug.Log("Done: "+generationData.RoomCount));
		}
		
		public void Generate(Action done)
		{
			workspaceRoot.ClearChildren();
			
			generationData = new GenerationData(12);
			
			StartCoroutine(OnGenerate(done));
		}
		
		IEnumerator OnGenerate(Action done)
		{
			var root = WorkspaceInstantiate(
				roomDefinitions,
				r => 4 <= r.DoorAnchors.Length
			);

			generationData.Rooms.Add(root);
			generationData.AvailableDoors.AddRange(AppendDoors(root));

			while (generationData.AvailableDoors.Any() && generationData.RoomCount < generationData.RoomCountMaximum)
			{
				var door = generationData.AvailableDoors.Random();
				yield return StartCoroutine(OnGenerate(door));
			}

			// var expectedDoorCount = generationData.Rooms.Sum(r => r.DoorAnchors.Length);
			//
			// if (expectedDoorCount != generationData.Doors.Count)
			// {
			// 	Debug.LogError("Expected " + expectedDoorCount + " doors, but generated " + generationData.Doors.Count);
			// }
			
			done();
		}

		IEnumerator OnGenerate(CollisionResolverDefinition originDoor)
		{
			var generationSuccess = false;
			
			for (var i = 0; i < RoomGenerationTimeouts; i++)
			{
				var possibleRoom = WorkspaceInstantiate(
					roomDefinitions,
					zeroSiblingIndex: true
				);

				for (var doorIndex = 0; doorIndex < possibleRoom.DoorAnchors.Length; doorIndex++)
				{
					var door = possibleRoom.DoorAnchors[doorIndex];
					
					possibleRoom.Dock(
						door,
						originDoor.DoorAnchors.Last()
					);
					
					yield return null;

					if (!possibleRoom.HasCollisions())
					{
						door.Id = possibleRoom.Id;

						possibleRoom.DoorAnchors[doorIndex] = door;
						
						break;
					}
				}

				if (!possibleRoom.HasCollisions())
				{
					generationData.AvailableDoors.Remove(originDoor);
					generationData.Doors.Add(originDoor);
					
					originDoor.DoorAnchors[1].Id = possibleRoom.Id;

					generationData.AvailableDoors.AddRange(
						AppendDoors(possibleRoom)	
					);
					
					generationData.Rooms.Add(possibleRoom);

					generationSuccess = true;
					break;
				}

				Destroy(possibleRoom.gameObject);
			}

			if (!generationSuccess)
			{
				generationData.AvailableDoors.Remove(originDoor);
				generationData.Doors.Add(originDoor);
			}
		}

		CollisionResolverDefinition[] AppendDoors(CollisionResolverDefinition room)
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
	}
}