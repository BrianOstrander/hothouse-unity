using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Models.AgentModels;
using UnityEngine;
using UnityEngine.AI;

namespace Lunra.Hothouse.Ai
{
	public class DwellerIdleState : AgentState<GameModel, DwellerModel>
	{
		public override string Name => "Idle";

		public override void OnInitialize()
		{
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