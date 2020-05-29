using System;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Models.AgentModels;
using UnityEngine;

namespace Lunra.Hothouse.Ai
{
	public class DwellerAttackState<S> : AgentState<GameModel, DwellerModel>
		where S : AgentState<GameModel, DwellerModel>
	{
		public override string Name => "Attack";

		public struct Target
		{
			public readonly Func<string> GetId;
			public readonly Func<float> GetHealth;
			public readonly Action<float> SetHealth;
			public readonly Action Killed;

			public Target(
				Func<string> getId,
				Func<float> getHealth,
				Action<float> setHealth,
				Action killed
			)
			{
				GetId = getId;
				GetHealth = getHealth;
				SetHealth = setHealth;
				Killed = killed;
			}
		}

		string initialTargetId;
		Target target;

		float cooldownElapsed;

		public override void OnInitialize()
		{
			AddTransitions(
				new ToReturnOnTargetIdMismatch(),
				new ToReturnOnTargetKilled()
			);
		}
		
		public void SetTarget(Target target)
		{
			initialTargetId = target.GetId();
			this.target = target;
		}

		public override void Idle()
		{
			cooldownElapsed += World.SimulationDelta;

			if (cooldownElapsed < Agent.MeleeCooldown.Value) return;

			cooldownElapsed = cooldownElapsed % Agent.MeleeCooldown.Value;

			var oldHealth = target.GetHealth();

			if (Mathf.Approximately(0f, oldHealth)) return;
			
			var newHealth = Mathf.Max(0f, oldHealth - Agent.MeleeDamage.Value);
			
			target.SetHealth(newHealth);

			if (Mathf.Approximately(0f, newHealth)) target.Killed();
		}

		public override void End()
		{
			initialTargetId = null;
			target = default;
			cooldownElapsed = 0f;
		}

		class ToReturnOnTargetIdMismatch : AgentTransition<DwellerAttackState<S>, S, GameModel, DwellerModel>
		{
			public override bool IsTriggered() => SourceState.initialTargetId != SourceState.target.GetId();
		}
		
		class ToReturnOnTargetKilled : AgentTransition<DwellerAttackState<S>, S, GameModel, DwellerModel>
		{
			public override bool IsTriggered() => Mathf.Approximately(0f, SourceState.target.GetHealth());
		}
	}
}