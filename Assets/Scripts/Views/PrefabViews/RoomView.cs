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
	public class RoomView : PrefabView, IRoomIdView
	{
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		[SerializeField] Light[] lights;
		[SerializeField] AnimationCurve lightIntensityByTimeOfDay;
		[FormerlySerializedAs("unexploredRoot")] [SerializeField] GameObject notRevealedRoot;

		[SerializeField] Transform[] doorAnchors = new Transform[0];
		[SerializeField] RoomCollider[] roomColliders = new RoomCollider[0];
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
		
		public string RoomId { get; set; }

		public RoomCollider[] RoomColliders => roomColliders;
		public (Vector3 Position, Vector3 Forward)[] DoorAnchors => doorAnchors.Select(d => (d.position, d.forward)).ToArray();
		#endregion

		public override void Reset()
		{
			base.Reset();

			TimeOfDay = 0f;
			IsRevealed = false;
			RoomId = null;
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

			var roomCollidersResult = new List<RoomCollider>();
			
			foreach (var collider in notRevealedRoot.transform.GetDescendants<Collider>())
			{
				var roomCollider = new RoomCollider();
				roomCollider.Collider = collider;
				roomCollider.Position = collider.transform.position;
				roomCollider.Scale = collider.transform.lossyScale;
				roomCollider.Rotation = collider.transform.rotation;
				
				roomCollidersResult.Add(roomCollider);
			}

			roomColliders = roomCollidersResult.ToArray();
			
		}
#endif
	}

}