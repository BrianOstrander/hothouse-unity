using Lunra.Hothouse.Models;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class EatDesireState : DesireState<EatDesireState>
	{
		public override Desires Desire => Desires.Eat;
	}
}