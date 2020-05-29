using System;
using Lunra.Hothouse.Models;

namespace Lunra.Hothouse.Ai
{
	public interface IAgentTransition<W, A>
		where A : AgentModel
	{
		string Name { get; }
		W World { get; }
		A Agent { get; }
		Type TargetState { get; }
		
		bool IsTriggered();

		void Transition();
		
		void Initialize(W world, A agent, AgentState<W, A> sourceState);
	}
	
	public abstract class AgentTransition<S0, S1, W, A> : IAgentTransition<W, A>
		where S0 : AgentState<W, A>
		where S1 : AgentState<W, A>
		where A : AgentModel
	{
		public virtual string Name => GetType().Name;
		
		public W World { get; private set; }
		public A Agent { get; private set; }

		public Type TargetState => typeof(S1);
		public S0 SourceState { get; private set; }

		public abstract bool IsTriggered();

		public virtual void Transition() {}

		public virtual void Initialize(
			W world,
			A agent,
			AgentState<W, A> sourceState
		)
		{
			World = world;
			Agent = agent;
			SourceState = sourceState as S0;
		}
	}
	
	public abstract class AgentTransition<S, W, A> : AgentTransition<AgentState<W, A>, S, W, A>
		where S : AgentState<W, A>
		where A : AgentModel
	{ }
}