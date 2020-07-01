using Lunra.Hothouse.Models;

namespace Lunra.Hothouse.Ai.Dweller
{
	public abstract class ObligationHandlerState<S0, S1> : BaseObligationHandlerState<S0, S1, DwellerModel>
		where S0 : AgentState<GameModel, DwellerModel>
		where S1 : ObligationHandlerState<S0, S1>
	{ }
}