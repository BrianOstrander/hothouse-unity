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
	
	public interface ILightModel : IModel, IRoomPositionModel
	{
		#region Serialized
		ListenerProperty<bool> IsLight { get; }
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
		public static bool IsLightActive(this ILightModel target)
		{
			switch (target.LightState.Value)
			{
				case LightStates.Fueled:
				case LightStates.Extinguishing:
					return true;
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