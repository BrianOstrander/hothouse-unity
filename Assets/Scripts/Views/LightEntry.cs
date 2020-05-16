using System;
using Lunra.Core;
using UnityEngine;

namespace Lunra.Hothouse.Views
{
	[Serializable]
	public struct LightEntry
	{
		public Light Light;
		public FloatRange Intensity;
		public AnimationCurve IntensityCurve;
		public Gradient Color;

		public void Update(float time)
		{
			Light.intensity = Intensity.Evaluate(IntensityCurve.Evaluate(time));
			Light.color = Color.Evaluate(time);
		}
	}
}