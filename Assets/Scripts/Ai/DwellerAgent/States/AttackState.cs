using System;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Models.AgentModels;
using UnityEngine;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class AttackState<S> : AgentState<GameModel, DwellerModel>
		where S : AgentState<GameModel, DwellerModel>
	{
		public override string Name => "Attack";

		public struct Target
		{
			public readonly Func<string> GetId;
			public readonly Func<bool> IsTargetDestroyed;
			public readonly Func<Damage.Result> Attack;

			public Target(
				Func<string> getId,
				Func<bool> isTargetDestroyed,
				Func<Damage.Result> attack
			)
			{
				GetId = getId;
				IsTargetDestroyed = isTargetDestroyed;
				Attack = attack;
			}
		}

		string initialTargetId;
		Target target;

		float cooldownElapsed;

		public override void OnInitialize()
		{
			AddTransitions(
				new ToReturnOnTargetIdMismatch(),
				new ToReturnOnTargetDestroyed()
			);
		}
		
		public void SetTarget(Target target)
		{
			initialTargetId = target.GetId();
			this.target = target;
		}

		public override void Idle()
		{
			cooldownElapsed += Game.SimulationDelta;

			if (cooldownElapsed < Agent.MeleeCooldown.Value) return;

			cooldownElapsed = cooldownElapsed % Agent.MeleeCooldown.Value;

			target.Attack();
		}

		public override void End()
		{
			initialTargetId = null;
			target = default;
			cooldownElapsed = 0f;
		}

		class ToReturnOnTargetIdMismatch : AgentTransition<AttackState<S>, S, GameModel, DwellerModel>
		{
			public override bool IsTriggered() => SourceState.initialTargetId != SourceState.target.GetId();
		}
		
		class ToReturnOnTargetDestroyed : AgentTransition<AttackState<S>, S, GameModel, DwellerModel>
		{
			public override bool IsTriggered() => SourceState.target.IsTargetDestroyed();
		}
	}
}