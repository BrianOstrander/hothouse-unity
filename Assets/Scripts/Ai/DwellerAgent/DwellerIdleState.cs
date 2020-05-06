using System.Linq;
using Lunra.Core;
using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Models.AgentModels;
using UnityEngine;
using UnityEngine.AI;

namespace Lunra.WildVacuum.Ai
{
	public class DwellerIdleState : AgentState<GameModel, DwellerModel>
	{
		public override string Name => "Idle";

		public override void OnInitialize()
		{
			InstantiateJob<DwellerClearFloraJobState>();
			InstantiateDesire<DwellerSleepDesireState>();
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
			AddTransitions(state.GetToDesireOnShiftEnd);
		}
	}
}