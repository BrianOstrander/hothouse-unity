using System;
using System.Linq;
using Lunra.Hothouse.Models;
using UnityEngine;
using UnityEngine.Serialization;

namespace Lunra.Hothouse.Views
{
	public class DwellerView : AgentView
	{
		[Serializable]
		struct DesireParticles
		{
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
			public Desires Desire;
			public ParticleSystem Filled;
			public ParticleSystem Missed;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null

			public void Play(bool filled)
			{
				if (filled)
				{
					if (Filled != null) Filled.Play();
				}
				else
				{
					if (Missed != null) Missed.Play();
				}
			}

			public void Stop()
			{
				if (Filled != null) Filled.Stop();
				if (Missed != null) Missed.Stop();
			}
		}
		
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		[FormerlySerializedAs("desireParticles")] [SerializeField] DesireParticles[] desires;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion
		
		#region Bindings
		public void PlayDesire(Desires desire, bool filled)
		{
			desires.FirstOrDefault(d => d.Desire == desire).Play(filled);
		}
		#endregion
		
		public override void Cleanup()
		{
			base.Cleanup();
			
			foreach (var desire in desires) desire.Stop();
		}
	}
}