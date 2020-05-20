using System;
using Lunra.Hothouse.Models;

namespace Lunra.Hothouse.Ai
{
	public class AgentTransitionFallthrough<S, W, A> : AgentTransition<S, W, A>
		where S : AgentState<W, A>
		where A : AgentModel
	{
		public override string Name => name;

		string name;
		Func<bool> isTriggered;
		Action transition;

		public AgentTransitionFallthrough(
			string name,
			Func<bool> isTriggered = null,
			Action transition = null
		)
		{
			this.name = name;
			this.isTriggered = isTriggered;
			this.transition = transition;
		}

		public override bool IsTriggered() => isTriggered?.Invoke() ?? true;

		public override void Transition() => transition?.Invoke();
	}
}