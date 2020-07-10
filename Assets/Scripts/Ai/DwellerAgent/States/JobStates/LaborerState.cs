using Lunra.Hothouse.Models;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class LaborerState<S> : JobState<S, LaborerState<S>>
		where S : AgentState<GameModel, DwellerModel>
	{
		protected override Jobs Job => Jobs.Laborer;

		public override void OnInitialize()
		{
			AddChildStates(
				new CleanupState(),
				new DestroyMeleeHandlerState(),
				new ConstructAssembleHandlerState()
			);
			
			AddTransitions(
				new ToReturnOnJobChanged(),
				new ToReturnOnShiftEnd(),
				
				new DestroyMeleeHandlerState.ToObligationOnExistingObligation(),
				new ConstructAssembleHandlerState.ToObligationOnExistingObligation(),
				
				new DestroyMeleeHandlerState.ToObligationHandlerOnAvailableObligation(),
				new ConstructAssembleHandlerState.ToObligationHandlerOnAvailableObligation(),
				
				new CleanupState.ToCleanupOnItemsAvailable()
			);
		}
	}
}