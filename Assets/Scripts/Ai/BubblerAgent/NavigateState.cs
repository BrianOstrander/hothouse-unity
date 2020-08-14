using Lunra.Hothouse.Models;

namespace Lunra.Hothouse.Ai.Bubbler
{
	public class NavigateState<S> : BaseNavigateState<S, BubblerModel>
		where S : AgentState<GameModel, BubblerModel>
	{ }
}