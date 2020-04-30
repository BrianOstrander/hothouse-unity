using System.Collections.Generic;
using Lunra.WildVacuum.Models;

namespace Lunra.WildVacuum.Ai
{
    public abstract class AgentState<W, A>
        where A : AgentModel
    {
        public string Name => GetType().Name;
        
        public W World { get; private set; }
        public A Agent { get; private set; }

        public void Initialize(
            W world,
            A agent
        )
        {
            World = world;
            Agent = agent;
            
            OnInitialize();
            
            foreach (var transition in Transitions) transition.Initialize(world, agent);
        }

        public virtual void OnInitialize() { }

        public virtual void Begin() { }
        public virtual void Idle(float delta) { }
        public virtual void End() { }
     
        public List<IAgentTransition<W, A>> Transitions { get; } = new List<IAgentTransition<W, A>>();

        public void AddTransitions(params IAgentTransition<W, A>[] transitions) => Transitions.AddRange(transitions);
    }
}