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
}