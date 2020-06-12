using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using UnityEngine;

namespace Lunra.Hothouse.Views
{
	public class CollisionResolverDefinition : MonoBehaviour
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
			public Transform Anchor;

			public Connection(
				string id,
				Transform anchor
			)
			{
				Id = id;
				Anchor = anchor;
			}
		}

		public string Id;
		public Types Type;
		public int RootDistance;
		
		public Vector2 Size;
		public Collider[] Colliders;
		public Connection[] DoorAnchors;
		
		public int CollisionCount;
		
		public void Define(
			Types type,
			string prefabId,
			RoomCollider[] colliders,
			(Vector3 Position, Vector3 Forward)[] doorAnchors
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
						case BoxCollider boxRef:
							var box = child.AddComponent<BoxCollider>();
							box.center = boxRef.center;
							box.size = boxRef.size;
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

			Type = type;
			Size = new Vector2(
				colliderMaximumX - colliderMinimumX,
				colliderMaximumZ - colliderMinimumZ
			);

			Colliders = colliderList.ToArray();

			var doorAnchorList = new List<Transform>();
			
			foreach (var doorAnchorRef in doorAnchors)
			{
				var doorAnchor = new GameObject("DoorAnchor_"+doorAnchorList.Count).transform;
				doorAnchor.SetParent(transform);
				
				doorAnchor.localPosition = doorAnchorRef.Position;
				doorAnchor.forward = doorAnchorRef.Forward;
				
				doorAnchorList.Add(doorAnchor);
			}

			DoorAnchors = doorAnchorList.Select(d => new Connection(null, d)).ToArray();
		}

		public void Dock(
			Connection doorAnchor,
			Connection otherDoorAnchor
		)
		{
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