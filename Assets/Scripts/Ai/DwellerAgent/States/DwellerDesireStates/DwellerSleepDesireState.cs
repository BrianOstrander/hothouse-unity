using Lunra.Hothouse.Models;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class DwellerSleepDesireState : DwellerDesireState<DwellerSleepDesireState>
	{
		public override Desires Desire => Desires.Sleep;
	}
}