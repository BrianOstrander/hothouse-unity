using Lunra.Hothouse.Models;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class InventoryPromiseHandlerState<S> : AgentState<GameModel, DwellerModel>
		where S : AgentState<GameModel, DwellerModel>
	{
		TimeoutState timeoutInstance;
		InventoryPromiseComponent.ProcessResult processResult;
		
		public override void OnInitialize()
		{
			AddChildStates(
				new NavigateState(),
				timeoutInstance = new TimeoutState()
			);
			
			AddTransitions(
				new ToReturnOnNone(),
				new ToTimeoutOnTransfer(),
				new ToNavigateToReservation()
			);
		}

		public override void Begin()
		{
			processResult = Agent.InventoryPromises.Process();
		}

		public class ToInventoryPromiseHandlerOnAvailable : AgentTransition<S, InventoryPromiseHandlerState<S>, GameModel, DwellerModel>
		{
			public override bool IsTriggered() => Agent.InventoryPromises.All.Any();
		}

		class ToReturnOnNone : AgentTransition<InventoryPromiseHandlerState<S>, S, GameModel, DwellerModel>
		{
			public override bool IsTriggered() => SourceState.processResult.Action == InventoryPromiseComponent.ProcessResult.Actions.None;
		}

		class ToTimeoutOnTransfer : AgentTransition<InventoryPromiseHandlerState<S>, TimeoutState, GameModel, DwellerModel>
		{
			public override bool IsTriggered() => SourceState.processResult.Action == InventoryPromiseComponent.ProcessResult.Actions.Timeout;

			// TODO: Don't hardcode thise
			public override void Transition() => SourceState.timeoutInstance.ConfigureForInterval(DayTime.FromRealSeconds(0.5f));
		}
		
		class ToNavigateToReservation : AgentTransition<InventoryPromiseHandlerState<S>, NavigateState, GameModel, DwellerModel>
		{
			public override bool IsTriggered() => SourceState.processResult.Action == InventoryPromiseComponent.ProcessResult.Actions.Navigate;

			public override void Transition() => Agent.NavigationPlan.Value = SourceState.processResult.Navigation;
		}

		#region Child Classes
		class NavigateState : NavigateState<InventoryPromiseHandlerState<S>> { }
		class TimeoutState : TimeoutState<InventoryPromiseHandlerState<S>> { }
		#endregion
	}
}