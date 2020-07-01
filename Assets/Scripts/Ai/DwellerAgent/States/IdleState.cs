using Lunra.Hothouse.Models;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class IdleState : AgentState<GameModel, DwellerModel>
	{
		public override string Name => "Idle";

		public override void OnInitialize()
		{
			AddChildStates(
				new LaborerState<IdleState>(),
				new StockpilerState<IdleState>()
			);
			
			AddTransitions(
				new LaborerState<IdleState>.ToJobOnShiftBegin(),
				new StockpilerState<IdleState>.ToJobOnShiftBegin()	
			);
		}
	}
}