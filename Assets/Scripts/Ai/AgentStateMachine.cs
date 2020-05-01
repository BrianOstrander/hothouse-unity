using System.Collections.Generic;
using System.Linq;
using Lunra.WildVacuum.Models;
using UnityEngine;

namespace Lunra.WildVacuum.Ai
{
	public abstract class AgentStateMachine<W, A>
		where A : AgentModel
	{
		public string Name => (string.IsNullOrEmpty(Agent.Id.Value) ? "null_or_empty_id" : Agent.Id.Value) + "<" + GetType().Name + ">";
		
		public List<AgentState<W, A>> States { get; } = new List<AgentState<W, A>>();
		public AgentState<W, A> CurrentState { get; protected set; }
		
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
			
			foreach (var state in States) state.Initialize(World, Agent);

			if (agent.IsDebugging) Debug.Log(Name + ": entering default state " + CurrentState.Name);
		}
		
		protected virtual void OnInitialize() { }
		
		public void Update(float delta)
		{
			foreach (var transition in CurrentState.Transitions)
			{
				if (!transition.IsTriggered()) continue;

				var targetState = States.First(s => s.GetType() == transition.TargetState);
				
				CurrentState.End();
				transition.Transition();
				targetState.Begin();

				if (Agent.IsDebugging) Debug.Log(Name + ": " + CurrentState.Name + "." + transition.Name + " -> " + targetState.Name);
				
				CurrentState = targetState;
				return;
			}
			
			CurrentState.Idle(delta);
		}
	}
}