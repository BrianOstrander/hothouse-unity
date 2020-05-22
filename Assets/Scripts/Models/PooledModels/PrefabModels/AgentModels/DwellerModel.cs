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
		[JsonIgnore] public ListenerProperty<Jobs> Job { get; }

		[JsonProperty] DayTimeFrame jobShift = DayTimeFrame.Zero;
		[JsonIgnore] public ListenerProperty<DayTimeFrame> JobShift { get; }
		
		[JsonProperty] Desires desire;
		[JsonIgnore] public ListenerProperty<Desires> Desire { get; }
		
		[JsonProperty] Dictionary<Desires, float> desireDamage = new Dictionary<Desires, float>();
		[JsonIgnore] public ListenerProperty<Dictionary<Desires, float>> DesireDamage { get; }
		
		[JsonProperty] float meleeRange;
		[JsonIgnore] public ListenerProperty<float> MeleeRange { get; }
		
		[JsonProperty] float meleeCooldown;
		[JsonIgnore] public ListenerProperty<float> MeleeCooldown { get; }
		
		[JsonProperty] float meleeDamage;
		[JsonIgnore] public ListenerProperty<float> MeleeDamage { get; }
		
		[JsonProperty] float withdrawalCooldown;
		[JsonIgnore] public ListenerProperty<float> WithdrawalCooldown { get; }
		
		[JsonProperty] float depositCooldown;
		[JsonIgnore] public ListenerProperty<float> DepositCooldown { get; }
		
		[JsonProperty] float transferDistance;
		[JsonIgnore] public ListenerProperty<float> TransferDistance { get; }
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
			MeleeRange = new ListenerProperty<float>(value => meleeRange = value, () => meleeRange);
			MeleeCooldown = new ListenerProperty<float>(value => meleeCooldown = value, () => meleeCooldown);
			MeleeDamage = new ListenerProperty<float>(value => meleeDamage = value, () => meleeDamage);
			WithdrawalCooldown = new ListenerProperty<float>(value => withdrawalCooldown = value, () => withdrawalCooldown); 
			DepositCooldown = new ListenerProperty<float>(value => depositCooldown = value, () => depositCooldown);
			TransferDistance = new ListenerProperty<float>(value => transferDistance = value, () => transferDistance);
		}
	}
}