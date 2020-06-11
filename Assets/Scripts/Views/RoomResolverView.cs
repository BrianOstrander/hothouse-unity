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
			Func<CollisionResolverDefinition, bool> predicate = null
		)
		{
			var prefab = predicate == null ? pool.Random() : pool.Where(predicate).Random();
			
			if (prefab == null)
			{
				Debug.LogError("No valid definition found");
				return null;
			}

			return workspaceRoot.gameObject.InstantiateChild(prefab);
		}

		[ContextMenu("ReGenerate")]
		void ReGenerate()
		{
			Generate(() => Debug.Log("Done: "+generationData.RoomCount));
		}
		
		public void Generate(Action done)
		{
			workspaceRoot.ClearChildren();
			
			generationData = new GenerationData(32);
			
			StartCoroutine(OnGenerate(done));
		}
		
		IEnumerator OnGenerate(Action done)
		{
			var root = WorkspaceInstantiate(
				roomDefinitions,
				r => 4 <= r.DoorAnchors.Length
			);

			generationData.Rooms.Add(root);
			generationData.AvailableDoors.AddRange(AppendDoors(root, p => false));

			while (generationData.AvailableDoors.Any() && generationData.RoomCount < generationData.RoomCountMaximum)
			{
				var door = generationData.AvailableDoors.Random();
				yield return StartCoroutine(OnGenerate(door));
			}

			done();
		}

		IEnumerator OnGenerate(CollisionResolverDefinition originDoor)
		{
			var generationSuccess = false;
			
			for (var i = 0; i < RoomGenerationTimeouts; i++)
			{
				var possibleRoom = WorkspaceInstantiate(roomDefinitions);

				foreach (var door in possibleRoom.DoorAnchors)
				{
					possibleRoom.Dock(
						door,
						originDoor.DoorAnchors.Last()
					);
					
					yield return null;

					if (!possibleRoom.HasCollisions()) break;
				}

				if (!possibleRoom.HasCollisions())
				{
					generationData.AvailableDoors.Remove(originDoor);
					generationData.Doors.Add(originDoor);
					
					generationData.AvailableDoors.AddRange(
						AppendDoors(
							possibleRoom,
							p => Vector3.Distance(p, originDoor.DoorAnchors.Last().position) < 0.01f // TODO: Don't hardcode this
						)	
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

		CollisionResolverDefinition[] AppendDoors(
			CollisionResolverDefinition room,
			Func<Vector3, bool> ignoreDoor
		)
		{
			var results = new List<CollisionResolverDefinition>();
			
			foreach (var doorAnchor in room.DoorAnchors)
			{
				if (ignoreDoor(doorAnchor.position)) continue;

				var door = WorkspaceInstantiate(doorDefinitions);
				
				door.Dock(
					door.DoorAnchors.First(),
					doorAnchor
				);
				
				results.Add(door);
			}

			return results.ToArray();
		}
	}
}