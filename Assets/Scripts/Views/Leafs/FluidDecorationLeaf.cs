using Lunra.Core;
using Lunra.Hothouse.Models;
using UnityEngine;

namespace Lunra.Hothouse.Views
{
	public class FluidDecorationLeaf : MonoBehaviour
	{
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		[SerializeField] ParticleSystem particles;
		[SerializeField] AnimationCurve flowCurve = AnimationCurveExtensions.LinearNormal();
		[SerializeField] FloatRange flowRange = FloatRange.Normal;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion

		public float FlowRate
		{
			set
			{
				var emission = particles.emission;
				emission.rateOverTime = new ParticleSystem.MinMaxCurve(
					flowRange.Evaluate(flowCurve.Evaluate(value))	
				);
			}
		}
	}
}