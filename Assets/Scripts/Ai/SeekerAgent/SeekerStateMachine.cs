using System.Collections.Generic;
using Lunra.Hothouse.Models;

namespace Lunra.Hothouse.Ai.Seeker
{
	public class SeekerStateMachine : AgentStateMachine<GameModel, SeekerModel>
	{
		protected override List<AgentState<GameModel, SeekerModel>> GetStates()
		{
			return new List<AgentState<GameModel, SeekerModel>>
			{
				// (DefaultState = new DwellerIdleState())
			};
		}
	}
}