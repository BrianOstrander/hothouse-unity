using System.Linq;
using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Models.AgentModels;
using UnityEngine;

namespace Lunra.WildVacuum.Ai
{
	public class DwellerClearFloraState : AgentState<GameModel, DwellerModel>
	{
		FloraModel target;
		
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
			
			// target = World.Flora.GetActive().FirstOrDefault(
			// 	f => Mathf.Approximately(0f, Vector3.Distance(f.NavigationPoint.Value.Position, Agent.Position.Value)) 
			// );
		}

		public override void Idle(float delta)
		{
			target.Health.Value = Mathf.Max(0f, target.Health.Value - (30f * delta));
		}

		class ToIdleOnFloraNullOrCleared : AgentTransition<DwellerIdleState, GameModel, DwellerModel>
		{
			DwellerClearFloraState sourceState;

			public ToIdleOnFloraNullOrCleared(DwellerClearFloraState sourceState) => this.sourceState = sourceState;
			
			public override bool IsTriggered()
			{
				return sourceState.target == null || Mathf.Approximately(0f, sourceState.target.Health.Value);
			}
		}
	}
}