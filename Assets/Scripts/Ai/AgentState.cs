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
        
        public G Game { get; private set; }
        public A Agent { get; private set; }

        public void Initialize(
            G game,
            A agent
        )
        {
            Game = game;
            Agent = agent;
            
            OnInitialize();
            
            foreach (var transition in Transitions) transition.Initialize(game, agent, this);
        }

        public virtual void OnInitialize() { }

        public virtual void Begin() { }
        public virtual void Idle() { }
        public virtual void End() { }
        
        public void AddChildStates(params AgentState<G, A>[] childStates) => ChildStates.AddRange(childStates);
        
        public void AddTransitions(params IAgentTransition<G, A>[] transitions) => Transitions.AddRange(transitions);
    }
}