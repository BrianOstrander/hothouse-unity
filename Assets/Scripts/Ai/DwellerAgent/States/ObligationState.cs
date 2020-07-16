using Lunra.Hothouse.Models;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class ObligationState<S> : BaseObligationState<S, ObligationState<S>, DwellerModel>
		where S : AgentState<GameModel, DwellerModel>
	{ }
}