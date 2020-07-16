using Lunra.Hothouse.Models;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class InventoryRequestState<S> : BaseInventoryRequestState<S, InventoryRequestState<S>, DwellerModel>
		where S : AgentState<GameModel, DwellerModel>
	{ }
}