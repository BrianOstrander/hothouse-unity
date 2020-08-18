using System.Collections.Generic;
using Lunra.Hothouse.Models;

namespace Lunra.Hothouse.Ai.SnapCap
{
	public class SnapCapStateMachine : AgentStateMachine<GameModel, SnapCapModel>
	{
		protected override List<AgentState<GameModel, SnapCapModel>> GetStates()
		{
			return new List<AgentState<GameModel, SnapCapModel>>
			{
				(DefaultState = new IdleState())
			};
		}
	}
}