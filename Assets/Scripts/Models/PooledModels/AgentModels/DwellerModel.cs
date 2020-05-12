using System.Collections.Generic;
using Lunra.Core;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models.AgentModels
{
	public class DwellerModel : AgentModel
	{
		#region Serialized
		[JsonProperty] Jobs job;
		[JsonIgnore] public readonly ListenerProperty<Jobs> Job;

		[JsonProperty] DayTimeFrame jobShift = DayTimeFrame.Zero;
		[JsonIgnore] public readonly ListenerProperty<DayTimeFrame> JobShift;
		
		[JsonProperty] Desires desire;
		[JsonIgnore] public readonly ListenerProperty<Desires> Desire;
		
		[JsonProperty] Dictionary<Desires, float> desireDamage = new Dictionary<Desires, float>();
		[JsonIgnore] public readonly ListenerProperty<Dictionary<Desires, float>> DesireDamage;
		
		[JsonProperty] Inventory inventory = Models.Inventory.Empty;
		[JsonIgnore] public readonly ListenerProperty<Inventory> Inventory;

		[JsonProperty] InventoryCapacity inventoryCapacity = Models.InventoryCapacity.ByNone();
		[JsonIgnore] public readonly ListenerProperty<InventoryCapacity> InventoryCapacity;

		[JsonProperty] InventoryPromise inventoryPromise = Models.InventoryPromise.Default();
		[JsonIgnore] public readonly ListenerProperty<InventoryPromise> InventoryPromise;

		[JsonProperty] float meleeRange;
		[JsonIgnore] public readonly ListenerProperty<float> MeleeRange;
		
		[JsonProperty] float meleeCooldown;
		[JsonIgnore] public readonly ListenerProperty<float> MeleeCooldown;
		
		[JsonProperty] float meleeDamage;
		[JsonIgnore] public readonly ListenerProperty<float> MeleeDamage;
		
		[JsonProperty] float withdrawalCooldown;
		[JsonIgnore] public readonly ListenerProperty<float> WithdrawalCooldown;
		
		[JsonProperty] float depositCooldown;
		[JsonIgnore] public readonly ListenerProperty<float> DepositCooldown;
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
			JobShift = new ListenerProperty<DayTimeFrame>(value => jobShift = value, () => jobShift);
			Desire = new ListenerProperty<Desires>(value => desire = value, () => desire);
			DesireDamage = new ListenerProperty<Dictionary<Desires, float>>(value => desireDamage = value, () => desireDamage);
			Inventory = new ListenerProperty<Inventory>(value => inventory = value, () => inventory);
			InventoryCapacity = new ListenerProperty<InventoryCapacity>(value => inventoryCapacity = value, () => inventoryCapacity);
			InventoryPromise = new ListenerProperty<InventoryPromise>(value => inventoryPromise = value, () => inventoryPromise);
			MeleeRange = new ListenerProperty<float>(value => meleeRange = value, () => meleeRange);
			MeleeCooldown = new ListenerProperty<float>(value => meleeCooldown = value, () => meleeCooldown);
			MeleeDamage = new ListenerProperty<float>(value => meleeDamage = value, () => meleeDamage);
			WithdrawalCooldown = new ListenerProperty<float>(value => withdrawalCooldown = value, () => withdrawalCooldown); 
			DepositCooldown = new ListenerProperty<float>(value => depositCooldown = value, () => depositCooldown);
		}
	}
}