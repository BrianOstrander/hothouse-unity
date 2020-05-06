using Lunra.WildVacuum.Models;

namespace Lunra.WildVacuum.Ai
{
	public class DwellerSleepDesireState : DwellerDesireState<DwellerSleepDesireState>
	{
		public override Desires Desire => Desires.Sleep;
		
		
	}
}