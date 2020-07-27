using System;
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
		public static class Constants
		{
			
			public const string FloorKeyword = "floor";
			public const string GeometryRoot = "geometry_root";
			public const string UnexploredRoot = "unexplored_root";
			public const string BoundaryColliderRoot = "boundary_collider_root";
			
			public const string DoorAnchorPrefix = "door_anchor_";
			public const char DoorAnchorSizeTerminator = 'm';
			public const string DoorPlugPrefix = "door_plug";
			
			public const string UnexploredMaterialPath = "Materials/unexplored";
			public const string DefaultFloorMaterialPath = "Materials/default_floors";

			public static class Walls
			{
				public const float DistanceMaximum = 1f;
				public const float HeightMaximum = 4f;
				public const float HeightIncrements = HeightMaximum / 8f;
				public const float CastIncrement = 0.5f;
			}
		}

		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		[SerializeField] Transform unexploredRoot;

		[SerializeField] GameObject boundaryColliderRoot;
		[SerializeField] DoorCache[] doorDefinitions = new DoorCache[0];
		[SerializeField] WallCache[] wallDefinitions = new WallCache[0];
		[SerializeField] ColliderCache[] boundaryColliders = new ColliderCache[0];
		[SerializeField] RoomVisibilityLeaf[] visibilityLeaves = new RoomVisibilityLeaf[0];
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion
		
		#region Bindings
		public bool IsRevealed { set => unexploredRoot.gameObject.SetActive(!value); }

		public void UnPlugDoors(params int[] activeDoorIndices)
		{
			foreach (var door in doorDefinitions)
			{
				door.Plug.SetActive(!activeDoorIndices.Contains(door.Index));
			}

			foreach (var visibilityLeaf in visibilityLeaves)
			{
				visibilityLeaf.gameObject.SetActive(visibilityLeaf.CalculateVisibility(activeDoorIndices));
			}
		}
		#endregion

		#region Reverse Bindings
		public ColliderCache[] BoundaryColliders => boundaryColliders;
		public DoorCache[] DoorDefinitions => doorDefinitions;
		public WallCache[] WallDefinitions => wallDefinitions;
		#endregion

		public override void Cleanup()
		{
			base.Cleanup();

			IsRevealed = false;
			UnPlugDoors();
		}

#if UNITY_EDITOR
		protected override void OnCalculateCachedData()
		{
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

			foreach (var floorElement in transform.GetDescendants(d => !string.IsNullOrEmpty(d.name) && d.name.Contains(Constants.FloorKeyword)))
			{
				floorElement.gameObject.SetLayerRecursively(LayerMask.NameToLayer(LayerNames.Floor));	
			}
			
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

			visibilityLeaves = transform.GetDescendants<RoomVisibilityLeaf>().ToArray();

			wallDefinitions = CalculateWallDefinitions();
		}

		WallCache[] CalculateWallDefinitions()
		{
			const float MaximumHitDistance = 1000f;
			
			var results = new List<WallCache>();
			
			if (boundaryColliders == null || boundaryColliders.None())
			{
				Debug.LogError("No boundary colliders have been defined");
				return results.ToArray();
			}

			var physicsScene = gameObject.scene.GetPhysicsScene();

			var minimumHeight = float.MaxValue;
			var maximumHeight = float.MinValue;
			
			foreach (var boundaryCollider in boundaryColliders)
			{
				var minimumRay = new Ray(
					boundaryCollider.Position + (Vector3.down * MaximumHitDistance),
					Vector3.up
				);
				
				var maximumRay = new Ray(
					boundaryCollider.Position + (Vector3.up * MaximumHitDistance),
					Vector3.down
				);

				if (physicsScene.Raycast(minimumRay.origin, minimumRay.direction, out var minimumHit, layerMask: LayerMasks.RoomBoundary, queryTriggerInteraction: QueryTriggerInteraction.Collide))
				{
					minimumHeight = Mathf.Min(minimumHeight, minimumHit.point.y);
					
				}
				else Debug.Log("Did not hit minimum collider, unexpected");
				
				if (physicsScene.Raycast(maximumRay.origin, maximumRay.direction, out var maximumHit, layerMask: LayerMasks.RoomBoundary, queryTriggerInteraction: QueryTriggerInteraction.Collide))
				{
					maximumHeight = Mathf.Max(maximumHeight, maximumHit.point.y);
				}
				else Debug.Log("Did not hit maximum collider, unexpected");
			}

			var wallPoints = new List<WallPoint>();
			
			void addHit(
				Vector3 floorPosition,
				Vector3 wallNormal,
				float height,
				int? doorIndex
			)
			{
				wallPoints.Add(
					new WallPoint
					{
						Index = wallPoints.Count,
						Position = floorPosition,
						WallNormal = wallNormal,
						Height = height,
						DoorIndex = doorIndex
					}
				);
			}

			bool tryHitFloor(
				Vector3 origin,
				out RaycastHit hit
			)
			{
				return physicsScene.Raycast(
					origin,
					Vector3.down,
					out hit,
					layerMask: LayerMasks.Floor,
					queryTriggerInteraction: QueryTriggerInteraction.Collide
				);
			}

			void castFloor(
				Vector3 origin,
				Vector3 direction,
				Vector3 wallNormal
			)
			{
				bool hitFloor;
				var distance = -0.01f;

				do
				{
					// distance += WallCastIncrement;
	
					var currentOrigin = origin + (direction * distance);
					hitFloor = tryHitFloor(currentOrigin, out var hit);
					distance += Constants.Walls.CastIncrement;
					
					if (hitFloor)
					{
						float? wallHeightMaximum = null;

						var hasCheckedForDoor = false;
						int? doorIndex = null;
						
						for (var h = Constants.Walls.HeightIncrements; h < Constants.Walls.HeightMaximum; h += Constants.Walls.HeightIncrements)
						{
							var didHitWall = physicsScene.Raycast(
								currentOrigin + (Vector3.up * h),
								-wallNormal,
								out var hitWall,
								Constants.Walls.DistanceMaximum * 1.1f,
								layerMask: LayerMasks.DefaultAndFloor,
								queryTriggerInteraction: QueryTriggerInteraction.Collide
							);

							if (!didHitWall || hitWall.distance < (Constants.Walls.DistanceMaximum * 0.9f))
							{
								break;
							}

							if (!hasCheckedForDoor)
							{
								hasCheckedForDoor = true;

								var currentParentToCheck = hitWall.transform;
								while (currentParentToCheck != null)
								{
									try
									{
										doorIndex = DoorDefinitions.First(d => d.Plug.transform == currentParentToCheck).Index;
										break;
									}
									catch (InvalidOperationException)
									{
										currentParentToCheck = currentParentToCheck.parent;
									}
								}
							}
							
							wallHeightMaximum = h;
						}

						if (wallHeightMaximum.HasValue)
						{
							addHit(
								hit.point,
								wallNormal,
								wallHeightMaximum.Value,
								doorIndex
							);
						}
					}
				} while (hitFloor);
			}
			
			var geometryRoot = transform.GetFirstDescendantOrDefault(d => d.name == Constants.GeometryRoot);

			if (geometryRoot != null)
			{
				foreach (var mesh in geometryRoot.GetDescendants<MeshFilter>().Where(m => m.sharedMesh != null))
				{
					// TODO: Leafs may appear when certain filtering is done... we may need to complicate this logic.
					if (mesh.transform.GetAncestor<RoomVisibilityLeaf>() != null) continue;
					
					var verts = mesh.sharedMesh.vertices;
					var normals = mesh.sharedMesh.normals;
					for (var i = 0; i < verts.Length; i++)
					{
						var normal = mesh.transform.TransformDirection(normals[i]);

						if (0.25f < Mathf.Abs(Vector3.Dot(normal, Vector3.up))) continue;

						normal = normal.NewY(0f).normalized;
						var rotation = Quaternion.LookRotation(normal);
						var vert = mesh.transform.TransformPoint(verts[i]);

						// gizmoCache.Add(() => Gizmos.color = Color.green);
						
						var origin = vert + (normal * Constants.Walls.DistanceMaximum) + (Vector3.up * 0.1f);
						
						if (tryHitFloor(origin, out var hit))
						{
							var left = (rotation * Quaternion.AngleAxis(90f, Vector3.up)) * Vector3.forward;
							var right = (rotation * Quaternion.AngleAxis(-90f, Vector3.up)) * Vector3.forward;
							
							// gizmoCache.Add(() => Gizmos.DrawRay(origin, left));
							// gizmoCache.Add(() => Gizmos.DrawRay(origin, right));
							// gizmoCache.Add(() => Gizmos.DrawRay(origin, normal));
							
							castFloor(origin, left, normal);
							castFloor(origin, right, normal);
						}
					}
				}
			}
			else Debug.LogError("Unable to find " + Constants.GeometryRoot);

			var wallPointsCachedForCleanup = wallPoints.ToArray();
			wallPoints.Clear();

			// Make cohesive walls that don't include points that should belong to other points.
			foreach (var wallPoint in wallPointsCachedForCleanup)
			{
				if (wallPoints.Any(r => r.DoorIndex == wallPoint.DoorIndex && Vector3.Distance(r.Position, wallPoint.Position) < (Constants.Walls.CastIncrement * 0.5f))) continue;
				
				var allNearbyResults = wallPointsCachedForCleanup
					.Where(r => Vector3.Distance(r.Position, wallPoint.Position) < (Constants.Walls.CastIncrement * 1.1f))
					.ToArray();
				
				var averageDot = allNearbyResults.Sum(r => Vector3.Dot(r.WallNormal, Vector3.forward)) / allNearbyResults.Length;

				var selectedResult = allNearbyResults
					.Where(r => r.DoorIndex == wallPoint.DoorIndex && Vector3.Distance(r.Position, wallPoint.Position) < (Constants.Walls.CastIncrement * 0.5f))
					.OrderBy(r => Mathf.Abs(averageDot - Vector3.Dot(r.WallNormal, Vector3.forward)))
					.First();
				
				wallPoints.Add(selectedResult);
			}

			wallPointsCachedForCleanup = wallPoints.ToArray();
			wallPoints.Clear();

			// Remove points near the edges of doors and walls that overlap.
			foreach (var wallPoint in wallPointsCachedForCleanup)
			{
				if (wallPointsCachedForCleanup.Any(r => r.Index != wallPoint.Index && r.DoorIndex != wallPoint.DoorIndex && Vector3.Distance(r.Position, wallPoint.Position) < (Constants.Walls.CastIncrement * 0.5f)))
				{
					continue;
				}
				
				wallPoints.Add(wallPoint);
			}
			
			for (var i = 0; i < wallPoints.Count; i++)
			{
				var wallPoint = wallPoints[i];

				var possibleNeighbors = wallPoints
					.Where(r => r.DoorIndex == wallPoint.DoorIndex)
					.Where(r => r.Index != wallPoint.Index && Vector3.Distance(r.Position, wallPoint.Position) < (Constants.Walls.CastIncrement * 1.1f))
					// .Where(r => Mathf.Approximately(r.Height, wallPoint.Height))
					.Where(r => Mathf.Approximately(1f, Vector3.Dot(r.WallNormal, wallPoint.WallNormal)))
					.OrderBy(r => Vector3.Distance(r.Position, wallPoint.Position))
					.Select(r => r.Index)
					.ToList();
				
				if (2 < possibleNeighbors.Count) possibleNeighbors.RemoveRange(2, possibleNeighbors.Count - 2);

				wallPoint.Neighbors = possibleNeighbors.ToArray();
				
				wallPoints[i] = wallPoint;
			}

			// Get rid of wall points with no neighbors.
			wallPoints = wallPoints
				.Where(r => 0 < r.Neighbors.Length)
				.ToList();

			wallPointsCachedForCleanup = wallPoints.ToArray();
			wallPoints.Clear();

			// Remove edges with different heights
			foreach (var wallPoint in wallPointsCachedForCleanup)
			{
				if (1 < wallPoint.Neighbors.Length)
				{
					var anyNeighborsHigher = wallPoint.Neighbors
						.Select(n => wallPointsCachedForCleanup.First(w => w.Index == n))
						.Any(n => wallPoint.Height < n.Height);
					
					if (anyNeighborsHigher) continue;
				}
				
				wallPoints.Add(wallPoint);
			}
			

			var colliders = geometryRoot.GetDescendants<Collider>(c => !c.isTrigger);

			if (colliders.Any())
			{
				wallPointsCachedForCleanup = wallPoints.ToArray();
				wallPoints.Clear();
				// Remove wall points inside collisions
				foreach (var wallPoint in wallPointsCachedForCleanup)
				{
					var wallPointPositionToCheckInBounds = wallPoint.Position + (Vector3.up * 0.1f);
					var collides = false;
					foreach (var collider in colliders)
					{
						if (!collider.bounds.Contains(wallPointPositionToCheckInBounds)) continue;
						
						collides = collider.ClosestPointIsInside(wallPointPositionToCheckInBounds);
						if (collides) break;
					}

					if (!collides) wallPoints.Add(wallPoint);
				}
			}

			var wallPointTerminals = wallPoints
				.Where(r => r.Neighbors.Length == 1)
				.ToList();
			
			var wallPointTerminalsUsed = new List<int>();

			// Determine what terminals connect and assign them as neighbors, ignoring others along the way.
			foreach (var wallPointTerminal in wallPointTerminals)
			{
				if (wallPointTerminalsUsed.Any(w => w == wallPointTerminal.Index)) continue;
				wallPointTerminalsUsed.Add(wallPointTerminal.Index);
				
				WallPoint? wallPointNext = wallPointTerminal;
				var wallEnd = wallPointNext.Value;

				var wallPointTerminalMinimumHeight = float.MaxValue;

				do
				{
					try
					{
						wallPointTerminalMinimumHeight = Mathf.Min(wallPointTerminalMinimumHeight, wallPointNext.Value.Height);
						wallPointNext = wallPoints
							.Where(r => !wallPointTerminalsUsed.Contains(r.Index))
							.First(r => wallPointNext.Value.Neighbors.Contains(r.Index));
						wallEnd = wallPointNext.Value;
						wallPointTerminalsUsed.Add(wallEnd.Index);
					}
					catch (InvalidOperationException)
					{
						wallPointNext = null;
					}
				}
				while (wallPointNext.HasValue);
				
				var result = new WallCache();

				result.Index = results.Count;
				result.Begin = wallPointTerminal.Position;
				result.End = wallEnd.Position;
				result.Normal = wallPointTerminal.WallNormal;
				result.Height = wallPointTerminalMinimumHeight;
				result.DoorIndex = wallPointTerminal.DoorIndex;
				
				results.Add(result);
			}
			
			return results.ToArray();
		}
		
		public void ApplyDefaultMaterials()
		{
			
			var defaultFloorMaterial = Resources.Load<Material>(Constants.DefaultFloorMaterialPath);
			
			if (defaultFloorMaterial == null) Debug.LogError("Unable to find material at resources path: "+Constants.DefaultFloorMaterialPath);
			else
			{
				foreach (var floorElement in transform.GetDescendants<MeshRenderer>(d => !string.IsNullOrEmpty(d.name) && d.name.Contains(Constants.FloorKeyword)))
				{
					if (floorElement.sharedMaterial == defaultFloorMaterial) continue;
					Undo.RecordObject(floorElement, "Apply Default Materials");
					floorElement.sharedMaterial = defaultFloorMaterial;
				}
			}
			PrefabUtility.RecordPrefabInstancePropertyModifications(gameObject);
		}

		public void ToggleSpawnTag()
		{
			Undo.RecordObject(this, "Toggle Spawn Tag");
			
			if (PrefabTags.Contains(PrefabTagCategories.Room.Spawn)) PrefabTags = PrefabTags.ExceptOne(PrefabTagCategories.Room.Spawn).ToArray();
			else PrefabTags = PrefabTags.Append(PrefabTagCategories.Room.Spawn).ToArray();
			
			PrefabUtility.RecordPrefabInstancePropertyModifications(this);
		}

		void OnDrawGizmosSelected()
		{
			if (!Application.isPlaying) ViewGizmos.DrawDoorGizmo(doorDefinitions);
				
			if (wallDefinitions != null)
			{
				foreach (var wall in wallDefinitions)
				{
					Gizmos.color = Color.green;
					Gizmos.DrawLine(wall.Begin, wall.Begin + (Vector3.up * wall.Height));

					Gizmos.color = wall.DoorIndex.HasValue ? Color.cyan : Color.magenta;
					Gizmos.DrawLine(wall.Begin + (Vector3.up * 0.1f), wall.End + (Vector3.up * 0.1f));

					Gizmos.color = Color.yellow;
					Gizmos.DrawLine(wall.Begin, wall.Begin + wall.Normal);
				}
			}
		}
#endif
	}

}