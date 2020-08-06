using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using UnityEditor;
using UnityEngine;

namespace Lunra.Hothouse.Views
{
	public interface IClearableView : IPrefabView, IEnterableView
	{
		void Highlight();
		void Select();
		void Deselect();
		
		float MeleeRangeBonus { get; }
	}
	
	public class ClearableView : PrefabView, IClearableView
	{
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		[SerializeField] GameObject entrancesRoot;
		[SerializeField] Transform[] entrances = new Transform[0];
		[SerializeField] float meleeRangeBonus;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion

		#region Bindings
		public virtual void Highlight() => SetColor(color => color.NewV(0.5f));
		public virtual void Select() => SetColor(color => color.NewV(0.75f));
		public virtual void Deselect() => SetColor(color => color.NewV(1f));
		#endregion
		
		#region Reverse Bindings
		public float MeleeRangeBonus => meleeRangeBonus;
		public Transform[] Entrances => entrances;
		#endregion

		public override void Cleanup()
		{
			base.Cleanup();

			Deselect();
		}

		#region Utility
		protected void SetColor(Func<Color, Color> apply)
		{
			foreach (var renderer in gameObject.GetComponentsInChildren<MeshRenderer>()) renderer.material.color = apply(renderer.material.color);
		}
		#endregion

#if UNITY_EDITOR
		protected override void OnCalculateCachedData()
		{
			NormalizeMeshCollidersFromRoot();
			
			var boundaries = new List<(Vector3 Position, Vector3 Normal, bool Hit)>();

			var physicsScene = gameObject.scene.GetPhysicsScene();

			const float SampleDelta = 360f / 8f;
			const float SampleRadius = 100f;
			const float EntranceDistance = 1.5f;

			var origin = Vector3.up * 0.1f;
			
			for (var i = 0f; i < 360f; i += SampleDelta)
			{
				var direction = Quaternion.AngleAxis(i, Vector3.up) * Vector3.forward;
				var position = origin + (direction * EntranceDistance);
				
				var didHit = physicsScene.Raycast(
					origin + (direction * SampleRadius),
					-direction,
					out var hit,
					SampleRadius,
					LayerMasks.Default
				);
				
				if (didHit) position = hit.point + (direction * EntranceDistance);

				if (boundaries.Any(b => Vector3.Distance(b.Position, position) < EntranceDistance)) continue;
				
				boundaries.Add((position, direction, didHit));
			}

			if (boundaries.None(b => b.Hit))
			{
				boundaries.Clear();
				boundaries.Add((origin, Vector3.forward, false));
			}

			if (entrancesRoot != null) DestroyImmediate(entrancesRoot);
			
			entrancesRoot = new GameObject("entrances");
			entrancesRoot.transform.SetParent(RootTransform);

			var entrancesList = new List<Transform>();
			
			var index = 0;
			foreach (var boundary in boundaries)
			{
				var entrance = new GameObject("entrance_"+index);
				entrance.transform.SetParent(entrancesRoot.transform);
				entrance.transform.position = boundary.Position;
				entrance.transform.forward = boundary.Normal;
				
				entrancesList.Add(entrance.transform);
				
				index++;
			}

			entrances = entrancesList.ToArray();
		}

		void OnDrawGizmosSelected()
		{
			if (Application.isPlaying) return;
			
			Gizmos.color = Color.green;
			foreach (var entrance in entrances) Gizmos.DrawWireCube(entrance.position, Vector3.one * 0.1f);

			// if (Mathf.Approximately(0f, MeleeRangeBonus)) return;
			//
			// Handles.color = Color.green.NewA(0.3f);
			// Handles.DrawWireDisc(transform.position, Vector3.up, meleeRangeBonus);
		}
#endif
	}

}