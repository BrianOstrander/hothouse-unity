using System.Linq;
using Lunra.Core;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Lunra.Hothouse.Views
{
	public class BuildingView : PrefabView, ILightView, IEnterableView
	{
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		// [SerializeField] NavMeshModifier[] navigationModifiers = new NavMeshModifier[0];
		[FormerlySerializedAs("navigationModifierColliders"), SerializeField]
		Collider[] navigationColliders = new Collider[0];
		[SerializeField] float navigationCollidersRadius;
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
		public Transform[] Entrances => entrances;

		public bool NavigationCollisionContains(Vector3 position)
		{
			return Vector3.Distance(RootTransform.position.NewY(0f), position.NewY(0f)) < navigationCollidersRadius;
		}

		public float NavigationColliderRadius => navigationCollidersRadius;
		#endregion

		public override void Cleanup()
		{
			base.Cleanup();

			IsNavigationModified = false;
			LightFuelNormal = 0f;
		}
		
		[ContextMenu("Calculate Collider Radius")]
		void CalculateBoundingNavigationColliderRadius()
		{
#if UNITY_EDITOR
			Undo.RecordObject(this, "Calculate Bounding Navigation");

			navigationColliders = transform.GetDescendants<Collider>().ToArray();
			
			var result = 0f;
			foreach (var collider in navigationColliders)
			{
				var radius = collider.bounds.extents.NewY(0f).magnitude;
				if (result < radius) result = radius;
			}

			result += 1f;

			navigationCollidersRadius = result;

			entrances = transform.GetDescendants(d => d.name.ToLower().Contains("entrance")).ToArray();

			PrefabUtility.RecordPrefabInstancePropertyModifications(this);

			// Optional step in order to save the Scene changes permanently.
			//EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
#endif
		}

		void OnDrawGizmosSelected()
		{
#if UNITY_EDITOR
			Handles.color = Color.yellow;
			Handles.DrawWireDisc(
				transform.position,
				Vector3.up,
				lightRange
			);

			Handles.color = Color.red;
			Handles.DrawWireDisc(
				transform.position,
				Vector3.up,
				navigationCollidersRadius
			);
			
			var childLight = GetComponentInChildren<Light>();
			if (childLight == null) return;
			
			Handles.color = childLight.color;
			Handles.DrawWireDisc(
				childLight.transform.position.NewY(transform.position.y),
				Vector3.up,
				childLight.range
			);
#endif
		}
	}
}