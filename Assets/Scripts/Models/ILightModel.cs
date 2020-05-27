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
		ListenerProperty<bool> IsLightEnabled { get; }
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
		public static bool ReseltLightEnabled(this ILightModel target, bool defaultValue)
		{
			var wasLightEnabled = target.IsLightEnabled.Value;
			if (target.IsLight.Value)
			{
				switch (target.LightState.Value)
				{
					case LightStates.Fueled:
					case LightStates.Extinguishing:
						target.IsLightEnabled.Value = defaultValue;
						break;
					case LightStates.Extinguished:
						target.IsLightEnabled.Value = false;
						break;
					default:
						Debug.LogError("Unrecognized LightState: " + target.LightState.Value);
						break;
				}
			}

			return wasLightEnabled != target.IsLightEnabled.Value;
		}
		
		public static bool IsLightActive(this ILightModel target)
		{
			switch (target.LightState.Value)
			{
				case LightStates.Fueled:
				case LightStates.Extinguishing:
					return target.IsLightEnabled.Value;
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