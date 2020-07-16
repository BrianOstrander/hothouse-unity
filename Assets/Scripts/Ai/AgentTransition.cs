using System;
using Lunra.Hothouse.Models;

namespace Lunra.Hothouse.Ai
{
	public interface IAgentTransition<G, A>
		where A : AgentModel
	{
		string Name { get; }
		AgentStateMachine<G, A> StateMachine { get; }
		G Game { get; }
		A Agent { get; }
		
		AgentState<G, A> TransitionTargetState { get; }
		
		bool IsTriggered();

		void Transition();
		
		void Initialize(
			AgentStateMachine<G, A> stateMachine,
			AgentState<G, A> sourceState
		);
	}
	
	/// <summary>
	/// 
	/// </summary>
	/// <typeparam name="S0">Source State</typeparam>
	/// <typeparam name="S1">Target State</typeparam>
	/// <typeparam name="G"></typeparam>
	/// <typeparam name="A"></typeparam>
	public abstract class AgentTransition<S0, S1, G, A> : IAgentTransition<G, A>
		where S0 : AgentState<G, A>
		where S1 : AgentState<G, A>
		where A : AgentModel
	{
		public virtual string Name => GetType().Name;
		
		public AgentStateMachine<G, A> StateMachine { get; private set; }
		public G Game => StateMachine.Game;
		public A Agent => StateMachine.Agent;

		public S0 SourceState { get; private set; }
		public S1 TargetState { get; private set; }
		public AgentState<G, A> TransitionTargetState => TargetState;

		public abstract bool IsTriggered();

		public virtual void Transition() {}

		public virtual void Initialize(
			AgentStateMachine<G, A> stateMachine,
			AgentState<G, A> sourceState
		)
		{
			if (sourceState == null) throw new ArgumentNullException(nameof(sourceState));

			StateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
			SourceState = sourceState as S0 ?? throw new NullReferenceException("Unable to convert " + nameof(sourceState) + " of type " + sourceState.GetType().Name + " to type " + typeof(S0).Name);

			if (!StateMachine.TryGetState<S1>(out var targetState)) throw new NullReferenceException("Unable to find target state of type " + typeof(S1).Name);

			TargetState = targetState;
		}
	}
	
	public abstract class AgentTransition<S, G, A> : AgentTransition<AgentState<G, A>, S, G, A>
		where S : AgentState<G, A>
		where A : AgentModel
	{ }
}