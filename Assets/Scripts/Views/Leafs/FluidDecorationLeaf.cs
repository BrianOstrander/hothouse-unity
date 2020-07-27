using Lunra.Hothouse.Models;
using UnityEngine;

namespace Lunra.Hothouse.Views
{
	public class FluidDecorationLeaf
	{
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		[SerializeField] ParticleSystem[] particleSystems;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion

		public float FlowRate
		{
			set
			{
				foreach (var particleSystem in particleSystems)
				{
					// particleSystem.main.
				}
			}
		}
	}
}