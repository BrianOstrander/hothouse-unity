using System.Collections.Generic;
using System.Linq;
using Lunra.Hothouse.Models;
using UnityEngine;

namespace Lunra.Hothouse.Ai
{
	public abstract class AgentStateMachine<W, A>
		where A : AgentModel
	{
		public string Name => (string.IsNullOrEmpty(Agent.Id.Value) ? "null_or_empty_id" : Agent.Id.Value) + "<" + GetType().Name + ">";
		
		public List<AgentState<W, A>> States { get; } = new List<AgentState<W, A>>();
		
		public AgentState<W, A> DefaultState { get; protected set; }
		public AgentState<W, A> CurrentState { get; private set; }
		
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

			if (DefaultState == null)
			{
				Debug.LogError("No "+nameof(DefaultState)+" specified");
				return;
			}

			CurrentState = DefaultState;
			
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
					Debug.LogError("Transition " + transition.Name + " requested a state that could not be found: " + transition.TargetState);
					return;
				}
				
				CurrentState.End();
				transition.Transition();
				targetState.Begin();

				Agent.Context = new AgentContext(
					Name,
					CurrentState.Name,
					targetState.Name,
					transition.Name
				);
				
				if (Agent.IsDebugging) Debug.Log(Agent.Context);

				CurrentState = targetState;
				return;
			}
			
			CurrentState.Idle();
		}

		public void Reset()
		{
			if (CurrentState == DefaultState) return;
			
			CurrentState.End();
			DefaultState.Begin();

			Agent.Context = new AgentContext(
				Name,
				CurrentState.Name,
				DefaultState.Name,
				"< Reset >"
			);
				
			if (Agent.IsDebugging) Debug.Log(Agent.Context);

			CurrentState = DefaultState;
		}
	}
}