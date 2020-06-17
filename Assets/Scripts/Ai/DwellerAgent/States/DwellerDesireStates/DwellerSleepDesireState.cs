using Lunra.Hothouse.Models;

namespace Lunra.Hothouse.Ai
{
	public class DwellerSleepDesireState : DwellerDesireState<DwellerSleepDesireState>
	{
		public override Desires Desire => Desires.Sleep;
	}
}