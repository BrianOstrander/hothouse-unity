using Lunra.Hothouse.Models;

namespace Lunra.Hothouse.Ai.Bubbler
{
	public class TimeoutState<S> : BaseTimeoutState<S, BubblerModel>
		where S : AgentState<GameModel, BubblerModel>
	{ }
}