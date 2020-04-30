using System;
using Lunra.WildVacuum.Models;

namespace Lunra.WildVacuum.Ai
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
		
		void Initialize(W world, A agent);
	}
	
	public abstract class AgentTransition<S, W, A> : IAgentTransition<W, A>
		where S : AgentState<W, A>
		where A : AgentModel
	{
		public string Name => GetType().Name;
		
		public W World { get; private set; }
		public A Agent { get; private set; }

		public Type TargetState => typeof(S); 
		
		public abstract bool IsTriggered();

		public virtual void Transition() {}

		public void Initialize(
			W world,
			A agent
		)
		{
			World = world;
			Agent = agent;
		}
	}
}