using System.Collections.Generic;
using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Models.AgentModels;

namespace Lunra.WildVacuum.Ai
{
	public class DwellerStateMachine : AgentStateMachine<GameModel, DwellerModel>
	{
		protected override List<AgentState<GameModel, DwellerModel>> GetStates()
		{
			return new List<AgentState<GameModel, DwellerModel>>
			{
				(CurrentState = new DwellerIdleState())
			};
		}
	}
}