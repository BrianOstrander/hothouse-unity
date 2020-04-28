using Lunra.Core;
using Lunra.StyxMvp;
using UnityEngine;

namespace Lunra.WildVacuum.Views
{
	public class FloraView : View
	{
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		
		[SerializeField] float maximumHeight;
		[SerializeField] Transform heightRoot;
		[SerializeField] AnimationCurve reproductionDirectionMutationFalloff;
		
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion

		#region Bindings

		public float Age { set => heightRoot.localPosition = new Vector3(0f, maximumHeight * Mathf.Clamp01(value), 0f); }

		public bool IsReproducing
		{
			set
			{
				var hue = value ? 0.35f : 0f;
				foreach (var renderer in gameObject.GetComponentsInChildren<MeshRenderer>()) renderer.material.color = renderer.material.color.NewH(hue);
			}
		}
		
		#endregion
		
		#region Reverse Bindings

		public AnimationCurve ReproductionDirectionMutationFalloff => reproductionDirectionMutationFalloff;
		
		#endregion
		
		public override void Reset()
		{
			base.Reset();

			Age = 0f;
			IsReproducing = false;
		}
	}

}