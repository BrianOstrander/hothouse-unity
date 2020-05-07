using System;
using Lunra.WildVacuum.Models;

namespace Lunra.WildVacuum.Ai
{
	public class AgentTransitionFallthrough<S, W, A> : AgentTransition<S, W, A>
		where S : AgentState<W, A>
		where A : AgentModel
	{
		public override string Name => name;

		string name;
		Func<bool> isTriggered;

		public AgentTransitionFallthrough(
			string name,
			Func<bool> isTriggered = null
		)
		{
			this.name = name;
			this.isTriggered = isTriggered;
		}

		public override bool IsTriggered() => isTriggered?.Invoke() ?? true;
	}
}