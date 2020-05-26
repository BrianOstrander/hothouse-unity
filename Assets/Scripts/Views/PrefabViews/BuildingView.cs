using System;
using System.Linq;
using Lunra.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

namespace Lunra.Hothouse.Views
{
	public class BuildingView : PrefabView, ILightView
	{
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		// [SerializeField] NavMeshModifier[] navigationModifiers = new NavMeshModifier[0];
		[FormerlySerializedAs("navigationModifierColliders"), SerializeField]
		Collider[] navigationColliders = new Collider[0];
		[SerializeField] Transform[] entrances = new Transform[0];
		[SerializeField] LightEntry[] lights = new LightEntry[0];
		[SerializeField] ParticleSystem[] lightParticles = new ParticleSystem[0];
		[SerializeField] float lightRange;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion

		#region Bindings
		public float LightFuelNormal
		{
			set
			{
				foreach (var light in lights) light.Update(value);
				var isEmitting = !Mathf.Approximately(0f, value);
				foreach (var particle in lightParticles)
				{
					if (isEmitting && particle.isStopped) particle.Play();
					else if (!isEmitting && particle.isPlaying) particle.Stop();
				}
			}
		}

		public bool IsNavigationModified
		{
			set
			{
				// foreach (var behaviour in navigationModifiers) behaviour.enabled = value;
				foreach (var behaviour in navigationColliders) behaviour.enabled = value;
			}
		}
		#endregion
		
		#region Reverse Bindings
		public bool IsLight => !Mathf.Approximately(0f, lightRange);
		public float LightRange => lightRange;
		public Vector3[] Entrances => entrances.Select(e => e.position).ToArray();
		#endregion

		public override void Reset()
		{
			base.Reset();

			IsNavigationModified = false;
			LightFuelNormal = 0f;
		}

		void OnDrawGizmosSelected()
		{
			Handles.color = Color.yellow;
			Handles.DrawWireDisc(
				transform.position,
				Vector3.up,
				lightRange
			);

			var childLight = GetComponentInChildren<Light>();
			if (childLight == null) return;
			
			Handles.color = childLight.color;
			Handles.DrawWireDisc(
				childLight.transform.position.NewY(transform.position.y),
				Vector3.up,
				childLight.range
			);
		}
	}
}