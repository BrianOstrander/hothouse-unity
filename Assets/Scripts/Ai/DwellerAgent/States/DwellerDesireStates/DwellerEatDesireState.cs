using Lunra.Hothouse.Models;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class DwellerEatDesireState : DwellerDesireState<DwellerEatDesireState>
	{
		public override Desires Desire => Desires.Eat;
	}
}