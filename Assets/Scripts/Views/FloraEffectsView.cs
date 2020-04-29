using Lunra.StyxMvp;
using UnityEngine;

namespace Lunra.WildVacuum.Views
{
	public class FloraEffectsView : View
	{
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		[SerializeField] ParticleSystem spawnParticles;
		[SerializeField] ParticleSystem chopParticles;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion

		#region Bindings
		public void Spawn() => spawnParticles.Play();
		public void Chop() => chopParticles.Play();
		#endregion
		
		#region Reverse Bindings
		#endregion

		public override void Reset()
		{
			base.Reset();

			
		}
	}

}