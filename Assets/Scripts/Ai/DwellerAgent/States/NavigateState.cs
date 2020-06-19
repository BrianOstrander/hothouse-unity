using Lunra.Hothouse.Models;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class NavigateState<S> : BaseNavigateState<S, DwellerModel>
		where S : AgentState<GameModel, DwellerModel>
	{ }
}