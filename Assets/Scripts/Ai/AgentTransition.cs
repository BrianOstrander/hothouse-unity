using System;
using Lunra.Hothouse.Models;

namespace Lunra.Hothouse.Ai
{
	public interface IAgentTransition<G, A>
		where A : AgentModel
	{
		string Name { get; }
		G Game { get; }
		A Agent { get; }
		Type TargetState { get; }
		
		bool IsTriggered();

		void Transition();
		
		void Initialize(G game, A agent, AgentState<G, A> sourceState);
	}
	
	public abstract class AgentTransition<S0, S1, G, A> : IAgentTransition<G, A>
		where S0 : AgentState<G, A>
		where S1 : AgentState<G, A>
		where A : AgentModel
	{
		public virtual string Name => GetType().Name;
		
		public G Game { get; private set; }
		public A Agent { get; private set; }

		public Type TargetState => typeof(S1);
		public S0 SourceState { get; private set; }

		public abstract bool IsTriggered();

		public virtual void Transition() {}

		public virtual void Initialize(
			G game,
			A agent,
			AgentState<G, A> sourceState
		)
		{
			Game = game;
			Agent = agent;
			SourceState = sourceState as S0;
		}
	}
	
	public abstract class AgentTransition<S, G, A> : AgentTransition<AgentState<G, A>, S, G, A>
		where S : AgentState<G, A>
		where A : AgentModel
	{ }
}