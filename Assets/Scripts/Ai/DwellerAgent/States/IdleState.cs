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
				new StockpilerState<IdleState>(),
				new SmokerState<IdleState>(),
				new FarmerState<IdleState>()
			);
			
			AddTransitions(
				new LaborerState<IdleState>.ToJobOnShiftBegin(),
				new StockpilerState<IdleState>.ToJobOnShiftBegin(),
				new SmokerState<IdleState>.ToJobOnShiftBegin(),
				new FarmerState<IdleState>.ToJobOnShiftBegin()
			);
		}
	}
}