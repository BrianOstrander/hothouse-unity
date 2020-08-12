using System;
using System.Collections.Generic;
using Lunra.Core;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UIElements;

namespace Lunra.Hothouse.Models
{
	public class DwellerModel : AgentModel, IGoalModel, IAttackModel
	{
		#region Serialized
		[JsonProperty] string name;
		[JsonIgnore] public ListenerProperty<string> Name { get; }
		[JsonProperty] Jobs job;
		[JsonIgnore] public ListenerProperty<Jobs> Job { get; }

		[JsonProperty] DayTimeFrame jobShift = DayTimeFrame.Zero;
		[JsonIgnore] public ListenerProperty<DayTimeFrame> JobShift { get; }

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
		
		[JsonProperty] public GoalComponent Goals { get; private set; } = new GoalComponent();
		[JsonProperty] public GoalPromiseComponent GoalPromises { get; private set; } = new GoalPromiseComponent();
		[JsonProperty] public AttackComponent Attacks { get; private set; } = new AttackComponent();
		#endregion
		
		#region Non Serialized
		#endregion

		public DwellerModel()
		{
			Name = new ListenerProperty<string>(value => name = value, () => name);
			Job = new ListenerProperty<Jobs>(value => job = value, () => job);
			JobShift = new ListenerProperty<DayTimeFrame>(value => jobShift = value, () => jobShift);
			WithdrawalCooldown = new ListenerProperty<float>(value => withdrawalCooldown = value, () => withdrawalCooldown); 
			DepositCooldown = new ListenerProperty<float>(value => depositCooldown = value, () => depositCooldown);
			ObligationDistance = new ListenerProperty<float>(value => obligationDistance = value, () => obligationDistance);
			ObligationMinimumConcentrationDuration = new ListenerProperty<float>(value => obligationMinimumConcentrationDuration = value, () => obligationMinimumConcentrationDuration);
			TransferDistance = new ListenerProperty<float>(value => transferDistance = value, () => transferDistance);
			LowRationThreshold = new ListenerProperty<int>(value => lowRationThreshold = value, () => lowRationThreshold);
			Bed = new ListenerProperty<InstanceId>(value => bed = value, () => bed);
			Workplace = new ListenerProperty<InstanceId>(value => workplace = value, () => workplace);
			
			AppendComponents(
				Goals,
				GoalPromises,
				Attacks
			);
		}
	}
}