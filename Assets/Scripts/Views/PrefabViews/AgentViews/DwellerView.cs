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
			public Motives motive;
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
		[SerializeField] Transform throwRoot;
		[SerializeField] ParticleSystem glowstickParticles;
		[SerializeField] MeshRenderer meshRenderer;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion
		
		#region Bindings
		public Jobs Job
		{
			set
			{
				var color = Color.white;
				
				switch (value)
				{
					case Jobs.None:
						break;
					case Jobs.Laborer:
						color = Color.red;
						break;
					case Jobs.Stockpiler:
						color = Color.blue;
						break;
					case Jobs.Smoker:
						color = Color.yellow;
						break;
					case Jobs.Farmer:
						color = Color.green;
						break;
					default:
						color = Color.gray;
						Debug.LogError("Unrecognized Job: "+value);
						break;
				}

				var material = new Material(meshRenderer.material);
				material.color = color;

				meshRenderer.material = material;
			}
		}
		public void PlayDesire(Motives motive, bool filled)
		{
			desires.FirstOrDefault(d => d.motive == motive).Play(filled);
		}
		
		public void LaunchGlowstick(Vector3 direction)
		{
			throwRoot.transform.forward = direction;
			glowstickParticles.Emit(1);
		}
		#endregion
		
		public override void Cleanup()
		{
			base.Cleanup();
			
			foreach (var desire in desires) desire.Stop();
			
			glowstickParticles.Stop();
			Job = Jobs.None;
		}

		void OnDrawGizmosSelected()
		{
			Gizmos.color = Color.yellow;
			Gizmos.DrawLine(throwRoot.position, throwRoot.position + (throwRoot.forward * 2f));
		}
	}
}