using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Lunra.Hothouse.Views
{
	public class RoomView : PrefabView, IRoomIdView, IBoundaryView
	{
		static class Constants
		{
			public const string DoorAnchorPrefix = "door_anchor_";
			public const string DoorAnchorSizeTerminator = "m";
			public const string DoorPlugPrefix = "door_plug";
		}

		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		[SerializeField] Transform unexploredRoot;

		[SerializeField] GameObject boundaryColliderRoot;
		[SerializeField] DoorCache[] doorDefinitions = new DoorCache[0];
		[SerializeField] ColliderCache[] boundaryColliders = new ColliderCache[0];
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion
		
		#region Bindings
		public bool IsRevealed { set => unexploredRoot.gameObject.SetActive(!value); }

		public void UnPlugDoors(params int[] plugIds)
		{
			foreach (var door in doorDefinitions)
			{
				door.Plug.SetActive(!plugIds.Contains(door.Index));
			}
		}

		public ColliderCache[] BoundaryColliders => boundaryColliders;
		public DoorCache[] DoorDefinitions => doorDefinitions;
		#endregion

		public override void Cleanup()
		{
			base.Cleanup();

			IsRevealed = false;
			UnPlugDoors();
		}

#if UNITY_EDITOR
		void OnDrawGizmosSelected()
		{
			if (doorDefinitions == null) return;

			Handles.color = Color.yellow.NewA(0.5f);

			foreach (var door in doorDefinitions)
			{
				if (door.Anchor == null) continue;

				var anchor = door.Anchor.position;
				var edge0 = door.Anchor.position + (door.Anchor.right * (door.Size * 0.5f));
				var edge1 = door.Anchor.position + (door.Anchor.right * (door.Size * -0.5f));
				var edgeForward = door.Anchor.position + door.Anchor.forward * 2f;
				var edgeUp = door.Anchor.position + door.Anchor.up;
				
				var offset = door.Anchor.forward * 0.1f;

				anchor += offset;
				edge0 += offset;
				edge1 += offset;
				edgeForward += offset;
				edgeUp += offset;
				
				
				Gizmos.color = Color.yellow;
				
				Gizmos.DrawLine(edge0, edge1);
				Gizmos.DrawLine(edge0, edgeForward);
				Gizmos.DrawLine(edge1, edgeForward);
				
				// Gizmos.DrawLine(anchor, edgeForward);
				Gizmos.DrawLine(edge0, edge0 + (Vector3.up * 4f));
				Gizmos.DrawLine(edge1, edge1 + (Vector3.up * 4f));
				
				Handles.DrawWireDisc(edge0, Vector3.up, 0.2f);
			}
		}
		
		public void CalculateCachedData()
		{
			Undo.RecordObject(this, "Calculate Cached Data");
			
			var doorDefinitionsList = new List<DoorCache>();

			int doorId = 0;
			foreach (var doorAnchor in transform.GetDescendants(c => !string.IsNullOrEmpty(c.name) && c.name.Contains(Constants.DoorAnchorPrefix)))
			{
				var doorAnchorSizeSerialized = doorAnchor.name.Substring(Constants.DoorAnchorPrefix.Length, doorAnchor.name.Length - Constants.DoorAnchorPrefix.Length);
				if (string.IsNullOrEmpty(doorAnchorSizeSerialized) || !doorAnchorSizeSerialized.Contains(Constants.DoorAnchorSizeTerminator))
				{
					Debug.LogError("Unable to parse door anchor size, no terminator detected: "+doorAnchor.name, doorAnchor);
					continue;
				}

				doorAnchorSizeSerialized = doorAnchorSizeSerialized.Split('m').FirstOrDefault();

				if (string.IsNullOrEmpty(doorAnchorSizeSerialized))
				{
					Debug.LogError("Unable to parse door anchor size, null or empty result: "+doorAnchor.name, doorAnchor);
					continue;
				}

				if (!int.TryParse(doorAnchorSizeSerialized, out var doorAnchorSize))
				{
					Debug.LogError("Unable to parse door anchor size, could not deserialize: "+doorAnchor.name, doorAnchor);
					continue;
				}

				var doorPlug = doorAnchor.parent.GetFirstDescendantOrDefault(d => !string.IsNullOrEmpty(d.name) && d.name == Constants.DoorPlugPrefix);

				if (doorPlug == null)
				{
					Debug.LogError("Unable to find a plug for door: "+doorAnchor.name, doorAnchor);
					continue;
				}
				
				doorDefinitionsList.Add(
					new DoorCache
					{
						Index = doorId,
						Anchor = doorAnchor,
						Plug = doorPlug.gameObject,
						Size = doorAnchorSize
					}
				);

				doorId++;
			}

			doorDefinitions = doorDefinitionsList.ToArray();

			unexploredRoot = unexploredRoot == null ? transform.GetFirstDescendantOrDefault(d => d.name == "UnexploredRoot") : unexploredRoot;
			
			if (unexploredRoot == null)
			{
				Debug.LogError("Unable to calculate room colliders, no UnexploredRoot found");
				return;
			}
			
			unexploredRoot.gameObject.SetLayerRecursively(LayerMask.NameToLayer(LayerNames.Unexplored));

			var unexploredMaterial = Resources.Load<Material>("Materials/Unexplored");
			
			if (unexploredMaterial == null) Debug.LogError("Unable to find Unexplored material");
			else
			{
				foreach (var meshRender in unexploredRoot.GetDescendants<MeshRenderer>())
				{
					meshRender.sharedMaterial = unexploredMaterial;
				}
			}

			if (boundaryColliderRoot != null) DestroyImmediate(boundaryColliderRoot);
			
			boundaryColliderRoot = new GameObject("BoundaryColliderRoot");
			boundaryColliderRoot.transform.SetParent(RootTransform);

			var roomCollidersResult = new List<ColliderCache>();
			
			foreach (var sourceCollider in unexploredRoot.transform.GetDescendants<Collider>())
			{
				if (sourceCollider is MeshCollider meshCollider) meshCollider.convex = true;
				sourceCollider.isTrigger = true;
				
				var duplicateRoot = boundaryColliderRoot.InstantiateChildObject(
					sourceCollider.gameObject,
					sourceCollider.transform.localPosition,
					sourceCollider.transform.localScale,
					sourceCollider.transform.localRotation,
					sourceCollider.gameObject.activeSelf
				);

				duplicateRoot.name = sourceCollider.name;
				
				foreach (var component in duplicateRoot.GetComponents<Component>())
				{
					if (component.GetType() == typeof(Transform)) continue;
					if (component is Collider) continue;
					
					DestroyImmediate(component);
				}
				
				duplicateRoot.SetLayerRecursively(LayerMask.NameToLayer(LayerNames.RoomBoundary));

				var collider = duplicateRoot.GetComponent<Collider>();
				
				var colliderCache = new ColliderCache();
				colliderCache.Collider = collider;
				colliderCache.Position = collider.transform.position;
				colliderCache.Scale = collider.transform.lossyScale;
				colliderCache.Rotation = collider.transform.rotation;
				
				roomCollidersResult.Add(colliderCache);
			}

			boundaryColliders = roomCollidersResult.ToArray();
		}
#endif
	}

}