using System.Linq;
using UnityEngine;

namespace Lunra.Hothouse.Views
{
	public class BuildingView : PrefabView, ILightView
	{
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		[SerializeField] Transform[] entrances = new Transform[0];
		[SerializeField] LightEntry[] lights = new LightEntry[0];
		[SerializeField] ParticleSystem[] lightParticles = new ParticleSystem[0];
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
		#endregion
		
		#region Reverse Bindings
		public bool IsLight => lights != null && lights.Any();
		public float LightRadius => lights.Max(e => e.Light.range);
		public float LightIntensity => lights.Max(e => e.Intensity.Maximum);
		public Vector3[] Entrances => entrances.Select(e => e.position).ToArray();
		#endregion

		public override void Reset()
		{
			base.Reset();

			LightFuelNormal = 0f;
		}

	}
}