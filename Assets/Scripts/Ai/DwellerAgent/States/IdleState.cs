using Lunra.Hothouse.Models;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class IdleState : AgentState<GameModel, DwellerModel>
	{
		public override string Name => "Idle";

		public override void OnInitialize()
		{
			var timeoutState = new TimeoutState<IdleState>();
			
			AddChildStates(
				timeoutState,
				new InventoryRequestState<IdleState>(),
				new ObligationState<IdleState>()
			);
			AddTransitions(
				new InventoryRequestState<IdleState>.ToInventoryRequestOnPromises(),
				new ObligationState<IdleState>.ToObligationOnObligations()
			);
		}
	}
}