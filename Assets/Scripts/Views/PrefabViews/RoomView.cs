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
			
			public const string FloorRoot = "floor_root";
			public const string GeometryRoot = "geometry_root";
			public const string UnexploredRoot = "unexplored_root";
			public const string BoundaryColliderRoot = "boundary_collider_root";
			
			public const string DoorAnchorPrefix = "door_anchor_";
			public const char DoorAnchorSizeTerminator = 'm';
			public const string DoorPlugPrefix = "door_plug";
			
			public const string UnexploredMaterialPath = "Materials/Unexplored";
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
			if (!Application.isPlaying) ViewGizmos.DrawDoorGizmo(doorDefinitions);
		}
		
		public void CalculateCachedData()
		{
			Undo.RecordObject(this, "Calculate Cached Data");

			PrefabId = gameObject.name;

			var doorDefinitionsList = new List<DoorCache>();

			var doorId = 0;
			foreach (var door in transform.GetDescendants(c => !string.IsNullOrEmpty(c.name) && c.name.StartsWith(Constants.DoorAnchorPrefix)))
			{
				if (!door.gameObject.activeInHierarchy) continue;
				
				var doorAnchorSizeSerialized = door.name.Substring(Constants.DoorAnchorPrefix.Length, door.name.Length - Constants.DoorAnchorPrefix.Length);
				if (string.IsNullOrEmpty(doorAnchorSizeSerialized) || !doorAnchorSizeSerialized.Contains(Constants.DoorAnchorSizeTerminator))
				{
					Debug.LogError("Unable to parse door anchor size, no terminator detected: "+door.name, door);
					continue;
				}

				doorAnchorSizeSerialized = doorAnchorSizeSerialized.Split(Constants.DoorAnchorSizeTerminator).FirstOrDefault();

				if (string.IsNullOrEmpty(doorAnchorSizeSerialized))
				{
					Debug.LogError("Unable to parse door anchor size, null or empty result: "+door.name, door);
					continue;
				}

				if (!int.TryParse(doorAnchorSizeSerialized, out var doorAnchorSize))
				{
					Debug.LogError("Unable to parse door anchor size, could not deserialize: "+door.name, door);
					continue;
				}

				var doorPlug = door.parent.GetFirstDescendantOrDefault(d => !string.IsNullOrEmpty(d.name) && d.name.StartsWith(Constants.DoorPlugPrefix));

				if (doorPlug == null)
				{
					Debug.LogError("Unable to find a plug for door: "+door.name, door);
					continue;
				}
				
				doorDefinitionsList.Add(
					new DoorCache
					{
						Index = doorId,
						Anchor = door,
						Plug = doorPlug.gameObject,
						Size = doorAnchorSize
					}
				);

				doorId++;
			}

			doorDefinitions = doorDefinitionsList.ToArray();

			unexploredRoot = unexploredRoot == null ? transform.GetFirstDescendantOrDefault(d => d.name == Constants.UnexploredRoot) : unexploredRoot;
			
			if (unexploredRoot == null)
			{
				Debug.LogError("Unable to calculate room colliders, could not find: "+Constants.UnexploredRoot);
				return;
			}
			
			unexploredRoot.gameObject.SetLayerRecursively(LayerMask.NameToLayer(LayerNames.Unexplored));
			
			NormalizeMeshColliders(unexploredRoot);

			var unexploredMaterial = Resources.Load<Material>(Constants.UnexploredMaterialPath);
			
			if (unexploredMaterial == null) Debug.LogError("Unable to find unexplored material at resources path: "+Constants.UnexploredMaterialPath);
			else
			{
				foreach (var meshRender in unexploredRoot.GetDescendants<MeshRenderer>())
				{
					meshRender.sharedMaterial = unexploredMaterial;
				}
			}

			var floorRoot = transform.GetFirstDescendantOrDefault(d => d.name == Constants.FloorRoot);

			if (floorRoot == null) Debug.LogError("Unable to set floor layers, could not find: "+Constants.FloorRoot);
			else floorRoot.gameObject.SetLayerRecursively(LayerMask.NameToLayer(LayerNames.Floor));
			
			var geometryRoot = transform.GetFirstDescendantOrDefault(d => d.name == Constants.GeometryRoot);
			
			if (geometryRoot == null) Debug.LogError("Unable to find: "+Constants.GeometryRoot);
			else NormalizeMeshColliders(geometryRoot);

			if (boundaryColliderRoot != null) DestroyImmediate(boundaryColliderRoot);
			
			boundaryColliderRoot = new GameObject(Constants.BoundaryColliderRoot);
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
			
			PrefabUtility.RecordPrefabInstancePropertyModifications(this);
		}

		void NormalizeMeshColliders(Transform root)
		{
			foreach (var mesh in root.GetDescendants<MeshFilter>())
			{
				var meshCollider = mesh.GetComponent<MeshCollider>();
				if (meshCollider == null) continue;
				if (meshCollider.sharedMesh != null && meshCollider.sharedMesh == mesh.sharedMesh) continue;
					
				meshCollider.sharedMesh = mesh.sharedMesh;
			}
		}
#endif
	}

}