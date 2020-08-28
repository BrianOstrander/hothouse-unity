using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Satchel;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
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
	
	public interface ILightModel : IInventoryModel
	{
		LightComponent Light { get; }
	}
	
	public class LightComponent : ComponentModel<ILightModel>
	{
		#region Serialized
		[JsonProperty] bool isLightEnabled; // TODO: Rename? Unify name?
		[JsonIgnore] public ListenerProperty<bool> IsLightCalculationsEnabled { get; }
		[JsonProperty] LightStates lightState;
		[JsonIgnore] public ListenerProperty<LightStates> LightState { get; }
		[JsonProperty] Stack[] lightFuel;
		[JsonIgnore] public ListenerProperty<Stack[]> LightFuel { get; }
		[JsonProperty] Interval lightFuelInterval;
		[JsonIgnore] public ListenerProperty<Interval> LightFuelInterval { get; }
		[JsonProperty] bool isLightRefueling;
		[JsonIgnore] public ListenerProperty<bool> IsLightRefueling { get; }
		#endregion

		#region Non Serialized
		bool isLight;
		[JsonIgnore] public ListenerProperty<bool> IsLight { get; }
		
		float lightRange;
		[JsonIgnore] public ListenerProperty<float> LightRange { get; }
		#endregion
		
		public LightComponent()
		{
			IsLightCalculationsEnabled = new ListenerProperty<bool>(value => isLightEnabled = value, () => isLightEnabled);
			LightState = new ListenerProperty<LightStates>(value => lightState = value, () => lightState);
			LightFuel = new ListenerProperty<Stack[]>(value => lightFuel = value, () => lightFuel);
			LightFuelInterval = new ListenerProperty<Interval>(value => lightFuelInterval = value, () => lightFuelInterval);
			IsLightRefueling = new ListenerProperty<bool>(value => isLightRefueling = value, () => isLightRefueling);
			
			IsLight = new ListenerProperty<bool>(value => isLight = value, () => isLight);
			LightRange = new ListenerProperty<float>(value => lightRange = value, () => lightRange);
		}
		
		public bool ReseltLightCalculationsEnabled(bool defaultValue)
		{
			var wasLightEnabled = IsLightCalculationsEnabled.Value;
			if (IsLight.Value)
			{
				switch (LightState.Value)
				{
					case LightStates.Fueled:
					case LightStates.Extinguishing:
						IsLightCalculationsEnabled.Value = defaultValue;
						break;
					case LightStates.Extinguished:
						IsLightCalculationsEnabled.Value = false;
						break;
					default:
						// TODO THIS SHOULD BE A ERROR EVENTUALLY
						Debug.LogWarning("Unrecognized LightState: " + LightState.Value);
						break;
				}
			}

			return wasLightEnabled != IsLightCalculationsEnabled.Value;
		}
		
		public bool IsLightActive()
		{
			if (!IsLight.Value) return false;
			
			switch (LightState.Value)
			{
				case LightStates.Fueled:
				case LightStates.Extinguishing:
					return IsLightCalculationsEnabled.Value;
				case LightStates.Extinguished:
					return false;
				default:
					// TODO THIS SHOULD BE A ERROR EVENTUALLY
					Debug.LogWarning("Unrecognized LightState: "+LightState.Value);
					return false;
			}
		}

		public bool IsLightNotActive() => !IsLightActive();

		public void Reset(
			Stack[] lightFuel,
			Interval lightFuelInterval,
			LightStates lightState
		)
		{
			IsLightCalculationsEnabled.Value = false;
			
			LightFuel.Value = lightFuel;
			LightFuelInterval.Value = lightFuelInterval;
			LightState.Value = lightState;
			
			IsLightRefueling.Value = true;
		}
		
		public override string ToString()
		{
			return "Light State: " + (IsLight.Value ? LightState.Value.ToString() : " < Not a Light >");
		}
	}
}