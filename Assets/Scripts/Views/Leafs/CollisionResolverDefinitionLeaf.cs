using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using UnityEngine;

namespace Lunra.Hothouse.Views
{
	public class CollisionResolverDefinitionLeaf : MonoBehaviour
	{
		public enum Types
		{
			Unknown = 0,
			Door = 10,
			Room = 20
		}

		[Serializable]
		public struct Connection
		{
			public string Id;
			public int Index;
			public int Size;
			public Transform Anchor;

			public Connection(
				string id,
				int index,
				int size,
				Transform anchor
			)
			{
				Id = id;
				Index = index;
				Size = size;
				Anchor = anchor;
			}
		}

		public string PrefabId;
		public string Id;
		public Types Type;
		public int RootDistance;
		
		public Vector2 Size;
		public Collider[] Colliders;
		public Connection[] DoorAnchors;
		public int DoorAnchorSizeMaximum;
		
		public int CollisionCount;
		
		public void Define(
			Types type,
			string prefabId,
			ColliderCache[] colliders,
			DoorCache[] doorAnchors
		)
		{
			var colliderList = new List<Collider>();

			var colliderMinimumX = 9999f;
			var colliderMinimumZ = colliderMinimumX;
			var colliderMaximumX = -colliderMinimumX;
			var colliderMaximumZ = colliderMaximumX;
			
			if (colliders != null)
			{
				foreach (var collider in colliders)
				{
					var child = new GameObject(prefabId + "_" + colliderList.Count);

					child.transform.SetParent(transform);

					child.transform.localPosition = collider.Position;
					child.transform.localScale = collider.Scale;
					child.transform.localRotation = collider.Rotation;

					switch (collider.Collider)
					{
						case BoxCollider boxReference:
							var box = child.AddComponent<BoxCollider>();
							box.center = boxReference.center;
							box.size = boxReference.size;
							break;
						case MeshCollider meshReference:
							var mesh = child.AddComponent<MeshCollider>();
							mesh.convex = meshReference.convex;
							mesh.cookingOptions = meshReference.cookingOptions;
							mesh.sharedMesh = meshReference.sharedMesh;
							break;
						default:
							Debug.LogError("Unrecognized collider of type: " + collider.Collider.GetType().Name);
							Destroy(child);
							continue;
					}

					var bounds = collider.Collider.bounds;

					colliderMinimumX = Mathf.Min(bounds.min.x, colliderMinimumX);
					colliderMinimumZ = Mathf.Min(bounds.min.z, colliderMinimumZ);
					colliderMaximumX = Mathf.Max(bounds.max.x, colliderMaximumX);
					colliderMaximumZ = Mathf.Max(bounds.max.z, colliderMaximumZ);
					
					colliderList.Add(child.GetComponent<Collider>());
				}

				var rigidbody = gameObject.AddComponent<Rigidbody>();
				rigidbody.constraints = RigidbodyConstraints.FreezeAll;
			}

			PrefabId = prefabId;
			Type = type;
			Size = new Vector2(
				colliderMaximumX - colliderMinimumX,
				colliderMaximumZ - colliderMinimumZ
			);

			Colliders = colliderList.ToArray();

			var doorAnchorsList = new List<Connection>();
			
			foreach (var doorAnchorReference in doorAnchors)
			{
				var doorAnchor = new GameObject("DoorAnchor_"+doorAnchorsList.Count).transform;
				doorAnchor.SetParent(transform);

				doorAnchor.localPosition = doorAnchorReference.Anchor.position;
				doorAnchor.forward = doorAnchorReference.Anchor.forward;
				
				doorAnchorsList.Add(
					new Connection(
						null,
						doorAnchorReference.Index,
						doorAnchorReference.Size,
						doorAnchor
					)	
				);
			}

			DoorAnchors = doorAnchorsList.ToArray();
			DoorAnchorSizeMaximum = doorAnchorsList.Max(d => d.Size);
		}

		public void Dock(
			Connection doorAnchor,
			Connection otherDoorAnchor
		)
		{
			var obj_door_0 = otherDoorAnchor.Anchor;
			var obj_root_1 = doorAnchor.Anchor.parent;
			var obj_door_1 = doorAnchor.Anchor;

			obj_root_1.rotation = Quaternion.Inverse(obj_door_1.localRotation);

			var dif = Quaternion.Angle(obj_door_0.rotation, obj_door_1.rotation);
		
			obj_root_1.rotation *= Quaternion.Euler(0f, dif + 180f, 0f);

			if (!Mathf.Approximately(-1f, Vector3.Dot(obj_door_0.forward, obj_door_1.forward)))
			{
				obj_root_1.rotation = Quaternion.Inverse(obj_door_1.localRotation);
				obj_root_1.rotation *= Quaternion.Euler(0f, 180 - dif, 0f);
			}

			obj_root_1.position = obj_door_0.position + (obj_root_1.position - obj_door_1.position);
			
			// var dot = Vector3.Dot(doorAnchor.Anchor.forward, otherDoorAnchor.Anchor.forward);
			//
			// Quaternion.LookRotation()
			/*
			transform.Rotate(Vector3.up, 180f - Vector3.Angle(doorAnchor.Anchor.forward, otherDoorAnchor.Anchor.forward));

			if (!Mathf.Approximately(180f, Vector3.Angle(doorAnchor.Anchor.forward, otherDoorAnchor.Anchor.forward)))
			{
				transform.Rotate(
					Vector3.up,
					Vector3.Angle(doorAnchor.Anchor.forward, otherDoorAnchor.Anchor.forward)
				);
			}
			
			if (Mathf.Approximately(1f, Vector3.Dot(doorAnchor.Anchor.forward, otherDoorAnchor.Anchor.forward)))
			{
				transform.Rotate(Vector3.up, 180f);
			}
			
			transform.position = otherDoorAnchor.Anchor.position + (transform.position - doorAnchor.Anchor.position);
			*/
		}

		public bool HasCollisions() => 0 < CollisionCount;
		
		#region Unity Events
		void OnCollisionEnter(Collision other) => CollisionCount++;

		void OnCollisionExit(Collision other) => CollisionCount--;
		#endregion
		
		void OnDrawGizmosSelected()
		{
			if (Type != Types.Door) return;
			if (DoorAnchors == null) return;
			if (DoorAnchors.Any(d => string.IsNullOrEmpty(d.Id))) return;
			
			/*
			Gizmos.color = Color.blue;
			foreach (var doorAnchor in DoorAnchors)
			{
				Gizmos.color = string.IsNullOrEmpty(doorAnchor.Id) ? Color.blue : Color.green;
				
				Gizmos.DrawWireSphere(doorAnchor.Anchor.position, 0.1f);
				Gizmos.DrawLine(doorAnchor.Anchor.position, doorAnchor.Anchor.position + doorAnchor.Anchor.forward);
			}
			*/
			
			foreach (var doorAnchor in DoorAnchors)
			{
				Gizmos.color = string.IsNullOrEmpty(doorAnchor.Id) ? Color.blue : Color.green;
				
				Gizmos.DrawLine(doorAnchor.Anchor.position, doorAnchor.Anchor.position - doorAnchor.Anchor.forward);
			}
		}
	}
}