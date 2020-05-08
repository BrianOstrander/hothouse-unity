using Lunra.WildVacuum.Models;

namespace Lunra.WildVacuum.Ai
{
	public class DwellerEatDesireState : DwellerDesireState<DwellerEatDesireState>
	{
		public override Desires Desire => Desires.Eat;
	}
}