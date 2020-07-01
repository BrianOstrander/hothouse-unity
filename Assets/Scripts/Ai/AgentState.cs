using System.Collections.Generic;
using Lunra.Hothouse.Models;

namespace Lunra.Hothouse.Ai
{
	public abstract class AgentState<G, A>
		where A : AgentModel
	{
		public virtual string Name => GetType().Name;

		public List<AgentState<G, A>> ChildStates { get; } = new List<AgentState<G, A>>();

		public List<IAgentTransition<G, A>> Transitions { get; } = new List<IAgentTransition<G, A>>();

		public AgentStateMachine<G, A> StateMachine { get; private set; }
		public G Game => StateMachine.Game;
		public A Agent => StateMachine.Agent;

		public void Initialize(
			AgentStateMachine<G, A> stateMachine
		)
		{
			StateMachine = stateMachine;

			OnInitialize();
		}

		public void InitializeTransitions()
		{
			foreach (var transition in Transitions)
			{
				transition.Initialize(StateMachine, this);
			}
		}

		public virtual void OnInitialize() { }

		public virtual void Begin() { }
		public virtual void Idle() { }
		public virtual void End() { }

		public void AddChildStates(params AgentState<G, A>[] childStates) => ChildStates.AddRange(childStates);

		public void AddTransitions(params IAgentTransition<G, A>[] transitions) => Transitions.AddRange(transitions);
	}
}