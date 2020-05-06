using Lunra.Core;
using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Models.AgentModels;

namespace Lunra.WildVacuum.Ai
{
	public abstract class DwellerDesireState<S> : AgentState<GameModel, DwellerModel>
		where S : DwellerDesireState<S>
	{
		public override string Name => Desire + "Desire";

		public abstract Desires Desire { get; }

		ToDesireOnShiftEnd toDesireOnShiftEnd;
		public ToDesireOnShiftEnd GetToDesireOnShiftEnd => toDesireOnShiftEnd ?? (toDesireOnShiftEnd = new ToDesireOnShiftEnd(this as S));
		
		public override void OnInitialize()
		{
			AddTransitions(
				new ToIdleOnDesireChanged(this as S),
				new ToIdleOnShiftBegin(this as S)
			);
		}

		public class ToDesireOnShiftEnd : AgentTransition<S, GameModel, DwellerModel>
		{
			public override string Name => base.Name + "<" + desireState.Name + ">";

			S desireState;

			public ToDesireOnShiftEnd(S desireState) => this.desireState = desireState; 
			
			public override bool IsTriggered()
			{
				switch (Agent.Desire.Value)
				{
					case Desires.Unknown:
					case Desires.None:
						return false;
				}
				
				return Agent.Desire.Value == desireState.Desire && !Agent.JobShift.Value.Contains(World.SimulationTime.Value);
			}
		}
		
		class ToIdleOnDesireChanged : AgentTransition<DwellerIdleState, GameModel, DwellerModel>
		{
			public override string Name => base.Name + "<" + desireState.Name + ">";
			
			S desireState;

			public ToIdleOnDesireChanged(S desireState) => this.desireState = desireState; 
			
			public override bool IsTriggered() => desireState.Desire != Agent.Desire.Value;
		}
		
		class ToIdleOnShiftBegin : AgentTransition<DwellerIdleState, GameModel, DwellerModel>
		{
			public override string Name => base.Name + "<" + desireState.Name + ">";
			
			S desireState;

			public ToIdleOnShiftBegin(S desireState) => this.desireState = desireState; 
			
			public override bool IsTriggered() => Agent.JobShift.Value.Contains(World.SimulationTime.Value);
		}
	}
}