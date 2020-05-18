using Lunra.Hothouse.Models;
using UnityEngine;

namespace Lunra.Hothouse.Utility
{
	public static class LightUtility
	{
		public static float CalculateIntensity(
			float distance,
			float range,
			float intensity
		)
		{
			if (range <= distance) return 0f;
			
			var result = intensity * Mathf.Pow(distance, 2f);

			if (Mathf.Approximately(0f, result)) return 0f;
			
			return 1f / result;
		}
	}
}