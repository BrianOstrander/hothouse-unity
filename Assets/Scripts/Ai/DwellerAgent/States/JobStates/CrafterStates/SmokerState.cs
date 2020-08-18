using Lunra.Hothouse.Models;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class SmokerState<S> : CrafterState<S, SmokerState<S>>
		where S : AgentState<GameModel, DwellerModel>
	{
		protected override Jobs Job => Jobs.Smoker;
	}
}