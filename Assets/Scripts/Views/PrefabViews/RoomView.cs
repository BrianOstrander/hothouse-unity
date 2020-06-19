using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Lunra.Hothouse.Views
{
	public class RoomView : PrefabView, IRoomIdView, IBoundaryView
	{
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		[SerializeField] Light[] lights;
		[SerializeField] AnimationCurve lightIntensityByTimeOfDay;
		[FormerlySerializedAs("unexploredRoot")] [SerializeField] GameObject notRevealedRoot;

		[SerializeField] GameObject boundaryColliderRoot;
		[SerializeField] Transform[] doorAnchors = new Transform[0];
		[SerializeField] ColliderCache[] boundaryColliders = new ColliderCache[0];
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion
		
		#region Bindings
		public float TimeOfDay
		{
			set
			{
				foreach (var light in lights) light.intensity = lightIntensityByTimeOfDay.Evaluate(value);
			}
		}

		public bool IsRevealed { set => notRevealedRoot.SetActive(!value); }

		public ColliderCache[] BoundaryColliders => boundaryColliders;
		public (Vector3 Position, Vector3 Forward)[] DoorAnchors => doorAnchors.Select(d => (d.position, d.forward)).ToArray();
		#endregion

		public override void Cleanup()
		{
			base.Cleanup();

			TimeOfDay = 0f;
			IsRevealed = false;
		}

		void OnDrawGizmosSelected()
		{
			if (doorAnchors == null) return;

			Gizmos.color = Color.blue;
			foreach (var doorAnchor in doorAnchors)
			{
				if (doorAnchor == null) continue;
				Gizmos.DrawRay(doorAnchor.position, doorAnchor.forward);
			}
		}

#if UNITY_EDITOR
		public void CalculateCachedData()
		{
			Undo.RecordObject(this, "Calculate Cached Data");
			
			doorAnchors = transform.GetDescendants(c => c.name == "DoorAnchor").ToArray();

			if (notRevealedRoot == null)
			{
				Debug.LogError("Unable to calculate room colliders because unexploredRoot is null");
				
				return;
			}
			
			if (boundaryColliderRoot != null) DestroyImmediate(boundaryColliderRoot);
			
			boundaryColliderRoot = new GameObject("BoundaryColliderRoot");
			boundaryColliderRoot.transform.SetParent(RootTransform);

			var roomCollidersResult = new List<ColliderCache>();
			
			foreach (var sourceCollider in notRevealedRoot.transform.GetDescendants<Collider>())
			{
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
				
				// duplicateRoot.name = sourceCollider.name;

				/*
				var roomCollider = new ColliderCache();
				roomCollider.Collider = collider;
				roomCollider.Position = collider.transform.position;
				roomCollider.Scale = collider.transform.lossyScale;
				roomCollider.Rotation = collider.transform.rotation;
				
				roomCollidersResult.Add(roomCollider);
				*/
			}

			boundaryColliders = roomCollidersResult.ToArray();
		}
#endif
	}

}