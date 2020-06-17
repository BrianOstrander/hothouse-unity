using Lunra.Hothouse.Models;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class SleepDesireState : DesireState<SleepDesireState>
	{
		public override Desires Desire => Desires.Sleep;
	}
}