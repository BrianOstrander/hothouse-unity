using System;
using System.Collections.Generic;
using Lunra.Core;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class DwellerModel : AgentModel, IGoalModel, IGoalPromiseModel
	{
		#region Serialized
		[JsonProperty] string name;
		[JsonIgnore] public ListenerProperty<string> Name { get; }
		[JsonProperty] Jobs job;
		[JsonIgnore] public ListenerProperty<Jobs> Job { get; }

		[JsonProperty] DayTimeFrame jobShift = DayTimeFrame.Zero;
		[JsonIgnore] public ListenerProperty<DayTimeFrame> JobShift { get; }
		
		[JsonProperty] Motives motive;
		[JsonIgnore] public ListenerProperty<Motives> Desire { get; }
		
		[JsonProperty] Dictionary<Motives, float> desireDamage = new Dictionary<Motives, float>();
		[JsonIgnore] public ListenerProperty<Dictionary<Motives, float>> DesireDamage { get; }
		
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

		[JsonProperty] InstanceId bed = InstanceId.Null();
		[JsonIgnore] public  ListenerProperty<InstanceId> Bed { get; }
		
		[JsonProperty] InstanceId workplace = InstanceId.Null();
		[JsonIgnore] public ListenerProperty<InstanceId> Workplace { get; }
		public GoalComponent Goals { get; } = new GoalComponent();
		public GoalPromiseComponent GoalPromises { get; } = new GoalPromiseComponent();
		#endregion
		
		#region Non Serialized
		[JsonIgnore] public Action<Motives, bool> DesireUpdated = ActionExtensions.GetEmpty<Motives, bool>();
		#endregion

		public bool GetDesireDamage(Motives motive, GameModel game, out float damage)
		{
			if (!DesireDamage.Value.TryGetValue(motive, out damage)) damage = 0f;
			else damage *= Health.Maximum.Value * game.DesireDamageMultiplier.Value;

			return !Mathf.Approximately(0f, damage);
		}
		
		public DwellerModel()
		{
			Name = new ListenerProperty<string>(value => name = value, () => name);
			Job = new ListenerProperty<Jobs>(value => job = value, () => job);
			JobShift = new ListenerProperty<DayTimeFrame>(value => jobShift = value, () => jobShift);
			Desire = new ListenerProperty<Motives>(value => motive = value, () => motive);
			DesireDamage = new ListenerProperty<Dictionary<Motives, float>>(value => desireDamage = value, () => desireDamage);
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
			Bed = new ListenerProperty<InstanceId>(value => bed = value, () => bed);
			Workplace = new ListenerProperty<InstanceId>(value => workplace = value, () => workplace);
		}
	}
}