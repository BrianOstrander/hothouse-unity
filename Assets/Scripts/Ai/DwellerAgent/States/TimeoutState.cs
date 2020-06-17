using Lunra.Hothouse.Models;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class TimeoutState<S> : BaseTimeoutState<S, DwellerModel>
		where S : AgentState<GameModel, DwellerModel>
	{ }
}