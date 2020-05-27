using System;
using Lunra.StyxMvp.Models;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public enum LightStates
	{
		Unknown = 0,
		Fueled = 10,
		Extinguishing = 20,
		Extinguished = 30
	}
	
	public interface ILightModel : IModel, IRoomTransform
	{
		#region Serialized
		ListenerProperty<bool> IsLight { get; }
		/// <summary>
		/// Is this light included in calculating lighting? If not that means this light may be fueled or extinguishing,
		/// but should be ignored when calculating lighting. This could occur, for example, when placing, constructing,
		/// or salvaging a light source -- if it's a building, which may not always be true.
		/// </summary>
		ListenerProperty<bool> IsLightCalculationsEnabled { get; }
		ListenerProperty<LightStates> LightState { get; }
		ListenerProperty<Inventory> LightFuel { get; }
		ListenerProperty<Interval> LightFuelInterval { get; }
		ListenerProperty<bool> IsLightRefueling { get; }
		#endregion

		#region Non Serialized
		ListenerProperty<float> LightRange { get; }
		#endregion
	}
	
	public static class LightModelExtensions
	{
		public static bool ReseltLightCalculationsEnabled(this ILightModel target, bool defaultValue)
		{
			var wasLightEnabled = target.IsLightCalculationsEnabled.Value;
			if (target.IsLight.Value)
			{
				switch (target.LightState.Value)
				{
					case LightStates.Fueled:
					case LightStates.Extinguishing:
						target.IsLightCalculationsEnabled.Value = defaultValue;
						break;
					case LightStates.Extinguished:
						target.IsLightCalculationsEnabled.Value = false;
						break;
					default:
						Debug.LogError("Unrecognized LightState: " + target.LightState.Value);
						break;
				}
			}

			return wasLightEnabled != target.IsLightCalculationsEnabled.Value;
		}
		
		public static bool IsLightActive(this ILightModel target)
		{
			switch (target.LightState.Value)
			{
				case LightStates.Fueled:
				case LightStates.Extinguishing:
					return target.IsLightCalculationsEnabled.Value;
				case LightStates.Extinguished:
					return false;
				default:
					Debug.LogError("Unrecognized LightState: "+target.LightState.Value);
					return false;
			}
		}

		public static bool IsLightNotActive(this ILightModel target) => !target.IsLightActive();
	}
}