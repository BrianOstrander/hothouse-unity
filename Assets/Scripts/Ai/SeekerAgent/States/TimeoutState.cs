using Lunra.Hothouse.Models;

namespace Lunra.Hothouse.Ai.Seeker
{
	public class TimeoutState<S> : BaseTimeoutState<S, SeekerModel>
		where S : AgentState<GameModel, SeekerModel>
	{ }
}