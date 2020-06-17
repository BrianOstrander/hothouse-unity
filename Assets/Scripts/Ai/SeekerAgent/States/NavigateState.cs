using Lunra.Hothouse.Models;

namespace Lunra.Hothouse.Ai.Seeker
{
	public class NavigateState<S> : BaseNavigateState<S, SeekerModel>
		where S : AgentState<GameModel, SeekerModel>
	{ }
}