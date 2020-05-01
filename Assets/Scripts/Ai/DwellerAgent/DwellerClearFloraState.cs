using System.Linq;
using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Models.AgentModels;
using UnityEngine;

namespace Lunra.WildVacuum.Ai
{
	public class DwellerClearFloraState : AgentState<GameModel, DwellerModel>
	{
		string targetId;
		FloraModel target;
		float cooldownElapsed;
		
		public override void OnInitialize()
		{
			AddTransitions(
				new ToIdleOnFloraNullOrCleared(this)	
			);
		}

		public override void Begin()
		{
			target = World.Flora.GetActive().OrderBy(
				f => Vector3.Distance(f.Position.Value, Agent.Position.Value)
			).FirstOrDefault();
			targetId = target?.Id.Value;
		}

		public override void Idle(float delta)
		{
			cooldownElapsed += delta;

			if (cooldownElapsed < Agent.MeleeCooldown.Value) return;

			cooldownElapsed = cooldownElapsed % Agent.MeleeCooldown.Value;
			
			target.Health.Value = Mathf.Max(0f, target.Health.Value - Agent.MeleeDamage.Value);
		}

		class ToIdleOnFloraNullOrCleared : AgentTransition<DwellerIdleState, GameModel, DwellerModel>
		{
			DwellerClearFloraState sourceState;

			public ToIdleOnFloraNullOrCleared(DwellerClearFloraState sourceState) => this.sourceState = sourceState;
			
			public override bool IsTriggered()
			{
				if (sourceState.target == null) return true;
				if (Mathf.Approximately(0f, sourceState.target.Health.Value)) return true;
				return sourceState.targetId != sourceState.target.Id.Value;
			}
		}
	}
}