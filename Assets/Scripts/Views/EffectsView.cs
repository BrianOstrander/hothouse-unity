using System.Collections.Generic;
using System.Linq;
using Lunra.StyxMvp;
using UnityEngine;
using UnityEngine.Serialization;

namespace Lunra.Hothouse.Views
{
	public class EffectsView : View
	{
		class Entry
		{
			public string Id;
			public ParticleSystem Particles;
		}
		
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		[SerializeField] Transform effectsRoot;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion
		
		List<Entry> entries;

		#region Bindings
		public void PlayEffect(
			Vector3 position,
			string id
		)
		{
			var entry = entries.FirstOrDefault(e => e.Id == id);

			if (entry == null)
			{
				Debug.LogError("Unrecognized effect id: "+id, effectsRoot);
				return;
			}
			
			entry.Particles.transform.position = position;
			entry.Particles.Play();	
		}
		#endregion
		
		protected override void OnPrepare()
		{
			base.OnPrepare();
			
			if (entries != null) return;
			
			entries = new List<Entry>();

			for (var i = 0; i < effectsRoot.childCount; i++)
			{
				var childParticleSystem = effectsRoot.GetChild(i).GetComponent<ParticleSystem>();

				if (childParticleSystem == null) continue;

				entries.Add(
					new Entry
					{
						Id = childParticleSystem.gameObject.name,
						Particles = childParticleSystem
					}	
				);
			}
		}
	}

}