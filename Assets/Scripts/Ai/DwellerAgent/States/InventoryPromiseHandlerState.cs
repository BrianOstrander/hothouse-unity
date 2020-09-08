using Lunra.Hothouse.Models;
using UnityEngine;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class InventoryPromiseHandlerState<S> : AgentState<GameModel, DwellerModel>
		where S : AgentState<GameModel, DwellerModel>
	{
		TimeoutState timeoutInstance;
		
		public override void OnInitialize()
		{
			AddChildStates(
				new NavigateState(),
				timeoutInstance = new TimeoutState()
			);
			
			AddTransitions(
				new ToReturnOnNoPromises()	
			);
		}

		public override void Begin()
		{
			Debug.Log("lol begin here");
		}

		public class ToInventoryPromiseHandlerOnAvailable : AgentTransition<S, InventoryPromiseHandlerState<S>, GameModel, DwellerModel>
		{
			public override bool IsTriggered() => Agent.InventoryPromises.All.Any();
		}

		protected class ToReturnOnNoPromises : AgentTransition<InventoryPromiseHandlerState<S>, S, GameModel, DwellerModel>
		{
			public override bool IsTriggered() => Agent.InventoryPromises.All.None();
		}
		
		#region Child Classes
		class NavigateState : NavigateState<InventoryPromiseHandlerState<S>> { }
		class TimeoutState : TimeoutState<InventoryPromiseHandlerState<S>> { }
		#endregion
	}
}