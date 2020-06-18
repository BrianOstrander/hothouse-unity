using System;
using Lunra.Core;
using Lunra.StyxMvp;
using UnityEngine;

namespace Lunra.Hothouse.Views
{
	public class FloraView : ClearableView
	{
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		
		[SerializeField] float maximumHeight;
		[SerializeField] Transform heightRoot;

		[SerializeField] string spawnEffectId;
		[SerializeField] string hurtEffectId;
		[SerializeField] string deathEffectId;
		
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion

		#region Bindings
		public float Age { set => heightRoot.localPosition = new Vector3(0f, maximumHeight * Mathf.Clamp01(value), 0f); }

		public bool IsReproducing
		{
			set
			{
				var saturation = value ? 0.47f : 0.37f;
				SetColor(color => color.NewS(saturation));
			}
		}
		#endregion
		
		#region Reverse Bindings
		public string SpawnEffectId => spawnEffectId;
		public string HurtEffectId => hurtEffectId;
		public string DeathEffectId => deathEffectId;
		#endregion

		public override void Reset()
		{
			base.Reset();

			Age = 0f;
			IsReproducing = false;
		}
	}

}