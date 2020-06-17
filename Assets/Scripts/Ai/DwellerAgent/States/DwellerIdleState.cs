using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Models.AgentModels;
using UnityEngine;
using UnityEngine.AI;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class DwellerIdleState : AgentState<GameModel, DwellerModel>
	{
		public override string Name => "Idle";

		public override void OnInitialize()
		{
			var timeoutState = new DwellerTimeoutState<DwellerIdleState>();
			
			AddChildStates(
				timeoutState	
			);
			AddTransitions(
				new DwellerDropItemsTransition<DwellerIdleState>(timeoutState)
			);
			
			InstantiateJob<DwellerClearerJobState>();
			InstantiateJob<DwellerConstructionJobState>();
			InstantiateDesire<DwellerSleepDesireState>();
			InstantiateDesire<DwellerEatDesireState>();
		}

		void InstantiateJob<S>()
			where S : DwellerJobState<S>, new()
		{
			var state = new S();
			AddChildStates(state);
			AddTransitions(state.GetToJobOnShiftBegin);
		}
		
		void InstantiateDesire<S>()
			where S : DwellerDesireState<S>, new()
		{
			var state = new S();
			AddChildStates(state);
			AddTransitions(new DwellerDesireState<S>.ToDesireOnShiftEnd(state));
		}
	}
}