using Lunra.Hothouse.Models;

namespace Lunra.Hothouse.Ai
{
	public class DwellerEatDesireState : DwellerDesireState<DwellerEatDesireState>
	{
		public override Desires Desire => Desires.Eat;
	}
}