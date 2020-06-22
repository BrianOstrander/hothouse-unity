using System;
using System.Collections.Generic;
using Lunra.Core;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class DwellerModel : AgentModel
	{
		#region Serialized
		[JsonProperty] string name;
		[JsonIgnore] public ListenerProperty<string> Name { get; }
		[JsonProperty] Jobs job;
		[JsonIgnore] public ListenerProperty<Jobs> Job { get; }

		[JsonProperty] DayTimeFrame jobShift = DayTimeFrame.Zero;
		[JsonIgnore] public ListenerProperty<DayTimeFrame> JobShift { get; }
		
		[JsonProperty] Desires desire;
		[JsonIgnore] public ListenerProperty<Desires> Desire { get; }
		
		[JsonProperty] Dictionary<Desires, float> desireDamage = new Dictionary<Desires, float>();
		[JsonIgnore] public ListenerProperty<Dictionary<Desires, float>> DesireDamage { get; }
		
		[JsonProperty] float desireMissedEmoteTimeout;
		[JsonIgnore] public ListenerProperty<float> DesireMissedEmoteTimeout { get; }
		
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
		
		[JsonProperty] float obligationDistance;
		[JsonIgnore] public ListenerProperty<float> ObligationDistance { get; }
		[JsonProperty] float obligationMinimumConcentrationDuration;
		[JsonIgnore] public ListenerProperty<float> ObligationMinimumConcentrationDuration { get; }
		
		[JsonProperty] float transferDistance;
		[JsonIgnore] public ListenerProperty<float> TransferDistance { get; }
		
		[JsonProperty] int lowRationThreshold;
		[JsonIgnore] public ListenerProperty<int> LowRationThreshold { get; }
		#endregion
		
		#region Non Serialized

		[JsonIgnore] public Action<Desires, bool> DesireUpdated = ActionExtensions.GetEmpty<Desires, bool>();
		#endregion

		public bool GetDesireDamage(Desires desire, GameModel game, out float damage)
		{
			if (!DesireDamage.Value.TryGetValue(desire, out damage)) damage = 0f;
			else damage *= Health.Maximum.Value * game.DesireDamageMultiplier.Value;

			return !Mathf.Approximately(0f, damage);
		}
		
		public DwellerModel()
		{
			Name = new ListenerProperty<string>(value => name = value, () => name);
			Job = new ListenerProperty<Jobs>(value => job = value, () => job);
			JobShift = new ListenerProperty<DayTimeFrame>(value => jobShift = value, () => jobShift);
			Desire = new ListenerProperty<Desires>(value => desire = value, () => desire);
			DesireDamage = new ListenerProperty<Dictionary<Desires, float>>(value => desireDamage = value, () => desireDamage);
			DesireMissedEmoteTimeout = new ListenerProperty<float>(value => desireMissedEmoteTimeout = value, () => desireMissedEmoteTimeout);
			MeleeRange = new ListenerProperty<float>(value => meleeRange = value, () => meleeRange);
			MeleeCooldown = new ListenerProperty<float>(value => meleeCooldown = value, () => meleeCooldown);
			MeleeDamage = new ListenerProperty<float>(value => meleeDamage = value, () => meleeDamage);
			WithdrawalCooldown = new ListenerProperty<float>(value => withdrawalCooldown = value, () => withdrawalCooldown); 
			DepositCooldown = new ListenerProperty<float>(value => depositCooldown = value, () => depositCooldown);
			ObligationDistance = new ListenerProperty<float>(value => obligationDistance = value, () => obligationDistance);
			ObligationMinimumConcentrationDuration = new ListenerProperty<float>(value => obligationMinimumConcentrationDuration = value, () => obligationMinimumConcentrationDuration);
			TransferDistance = new ListenerProperty<float>(value => transferDistance = value, () => transferDistance);
			LowRationThreshold = new ListenerProperty<int>(value => lowRationThreshold = value, () => lowRationThreshold);
		}
	}
}