using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Models.AgentModels;

namespace Lunra.WildVacuum.Ai
{
	public class DwellerStateMachine : AgentStateMachine<GameModel, DwellerModel>
	{
		protected override void OnInitialize()
		{
			States.AddRange(
				new []
				{
					CurrentState = new DwellerIdleState(),
					new DwellerNavigateState(),
					new DwellerClearFloraState()
				}
			);
		}
	}
}