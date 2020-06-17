using System.Collections.Generic;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Models.AgentModels;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class DwellerStateMachine : AgentStateMachine<GameModel, DwellerModel>
	{
		protected override List<AgentState<GameModel, DwellerModel>> GetStates()
		{
			return new List<AgentState<GameModel, DwellerModel>>
			{
				(DefaultState = new IdleState())
			};
		}
	}
}