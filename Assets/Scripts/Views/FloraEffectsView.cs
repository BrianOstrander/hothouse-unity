using Lunra.StyxMvp;
using UnityEngine;
using UnityEngine.Serialization;

namespace Lunra.Hothouse.Views
{
	public class FloraEffectsView : View
	{
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		[SerializeField] ParticleSystem spawnParticles;
		[SerializeField] ParticleSystem hurtParticles;
		[SerializeField] ParticleSystem deathParticles;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion

		#region Bindings
		public void PlaySpawn(Vector3 position)
		{
			spawnParticles.transform.position = position;
			spawnParticles.Play();	
		}
		
		public void PlayHurt(Vector3 position)
		{
			hurtParticles.transform.position = position;
			hurtParticles.Play();
		}

		public void PlayDeath(Vector3 position)
		{
			deathParticles.transform.position = position;
			deathParticles.Play();
		}
		#endregion
		
		#region Reverse Bindings
		#endregion

		public override void Reset()
		{
			base.Reset();

			
		}
	}

}