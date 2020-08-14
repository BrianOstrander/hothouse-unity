using System.Collections.Generic;
using Lunra.Hothouse.Models;

namespace Lunra.Hothouse.Ai.Bubbler
{
	public class BubblerStateMachine : AgentStateMachine<GameModel, BubblerModel>
	{
		protected override List<AgentState<GameModel, BubblerModel>> GetStates()
		{
			return new List<AgentState<GameModel, BubblerModel>>
			{
				(DefaultState = new IdleState())
			};
		}
	}
}