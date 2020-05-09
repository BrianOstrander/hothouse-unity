using System.Collections.Generic;
using Lunra.Core;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.WildVacuum.Models.AgentModels
{
	public class DwellerModel : AgentModel
	{
		#region Serialized
		[JsonProperty] Jobs job;
		[JsonIgnore] public readonly ListenerProperty<Jobs> Job;
		
		[JsonProperty] int jobPriority;
		[JsonIgnore] public readonly ListenerProperty<int> JobPriority;

		[JsonProperty] DayTimeFrame jobShift = DayTimeFrame.Zero;
		[JsonIgnore] public readonly ListenerProperty<DayTimeFrame> JobShift;
		
		[JsonProperty] Desires desire;
		[JsonIgnore] public readonly ListenerProperty<Desires> Desire;
		
		[JsonProperty] Dictionary<Desires, float> desireDamage = new Dictionary<Desires, float>();
		[JsonIgnore] public readonly ListenerProperty<Dictionary<Desires, float>> DesireDamage;
		
		[JsonProperty] Inventory inventory = Models.Inventory.Empty;
		[JsonIgnore] public readonly ListenerProperty<Inventory> Inventory;
		
		[JsonProperty] float meleeRange;
		[JsonIgnore] public readonly ListenerProperty<float> MeleeRange;
		
		[JsonProperty] float meleeCooldown;
		[JsonIgnore] public readonly ListenerProperty<float> MeleeCooldown;
		
		[JsonProperty] float meleeDamage;
		[JsonIgnore] public readonly ListenerProperty<float> MeleeDamage;
		
		[JsonProperty] float loadCooldown;
		[JsonIgnore] public readonly ListenerProperty<float> LoadCooldown;
		
		[JsonProperty] float unloadCooldown;
		[JsonIgnore] public readonly ListenerProperty<float> UnloadCooldown;
		#endregion
		
		#region Non Serialized
		#endregion

		public bool GetDesireDamage(Desires desire, out float damage)
		{
			if (!DesireDamage.Value.TryGetValue(desire, out damage)) damage = 0f;
			else damage *= HealthMaximum.Value;

			return !Mathf.Approximately(0f, damage);
		}
		
		public DwellerModel()
		{
			Job = new ListenerProperty<Jobs>(value => job = value, () => job);
			JobPriority = new ListenerProperty<int>(value => jobPriority = value, () => jobPriority);
			JobShift = new ListenerProperty<DayTimeFrame>(value => jobShift = value, () => jobShift);
			Desire = new ListenerProperty<Desires>(value => desire = value, () => desire);
			DesireDamage = new ListenerProperty<Dictionary<Desires, float>>(value => desireDamage = value, () => desireDamage);
			Inventory = new ListenerProperty<Inventory>(value => inventory = value, () => inventory);
			MeleeRange = new ListenerProperty<float>(value => meleeRange = value, () => meleeRange);
			MeleeCooldown = new ListenerProperty<float>(value => meleeCooldown = value, () => meleeCooldown);
			MeleeDamage = new ListenerProperty<float>(value => meleeDamage = value, () => meleeDamage);
			LoadCooldown = new ListenerProperty<float>(value => loadCooldown = value, () => loadCooldown); 
			UnloadCooldown = new ListenerProperty<float>(value => unloadCooldown = value, () => unloadCooldown);
		}
	}
}