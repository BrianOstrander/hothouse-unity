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
		
		public bool IsOutOfState { get; private set; }

		public void Initialize(
			W world,
			A agent
		)
		{
			World = world;
			Agent = agent;

			var statesRemainingToInitialize = GetStates();

			while (statesRemainingToInitialize.Any())
			{
				var state = statesRemainingToInitialize.First();
				statesRemainingToInitialize.RemoveAt(0);
				
				States.Add(state);
				
				state.Initialize(World, Agent);
				
				statesRemainingToInitialize.AddRange(state.ChildStates);
			}
			
			if (agent.IsDebugging) Debug.Log(Name + ": entering default state " + CurrentState.Name);
		}

		protected abstract List<AgentState<W, A>> GetStates();
		
		public void Update()
		{
			if (IsOutOfState) return;
			
			foreach (var transition in CurrentState.Transitions)
			{
				if (!transition.IsTriggered()) continue;

				var targetState = States.FirstOrDefault(s => s.GetType() == transition.TargetState);

				if (targetState == null)
				{
					IsOutOfState = true;
					Debug.LogError("Unable to find transition to: "+transition.TargetState);
					return;
				}
				
				CurrentState.End();
				transition.Transition();
				targetState.Begin();

				if (Agent.IsDebugging) Debug.Log(Name + ": " + CurrentState.Name + "." + transition.Name + " -> " + targetState.Name);
				
				CurrentState = targetState;
				return;
			}
			
			CurrentState.Idle();
		}
	}
}