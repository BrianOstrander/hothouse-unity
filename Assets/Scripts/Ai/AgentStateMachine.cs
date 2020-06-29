using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using UnityEngine;

namespace Lunra.Hothouse.Ai
{
	public abstract class AgentStateMachine<G, A>
		where A : AgentModel
	{
		public string Name => Agent.ShortId + "<" + GetType().Name + ">";
		
		public List<AgentState<G, A>> States { get; } = new List<AgentState<G, A>>();
		
		public AgentState<G, A> DefaultState { get; protected set; }
		public AgentState<G, A> CurrentState { get; private set; }
		
		public G Game { get; private set; }
		public A Agent { get; private set; }
		
		public bool IsOutOfState { get; private set; }

		public void Initialize(
			G game,
			A agent
		)
		{
			Game = game;
			Agent = agent;

			var statesRemainingToInitialize = GetStates();

			while (statesRemainingToInitialize.Any())
			{
				var state = statesRemainingToInitialize.First();
				statesRemainingToInitialize.RemoveAt(0);
				
				States.Add(state);
				
				state.Initialize(Game, Agent);
				
				statesRemainingToInitialize.AddRange(state.ChildStates);
			}

			if (DefaultState == null)
			{
				Debug.LogError("No "+nameof(DefaultState)+" specified for "+GetType().Name);
				return;
			}

			CurrentState = DefaultState;

			if (agent.IsDebugging)
			{
				Debug.Log(Name + ": entering default state " + CurrentState.Name);
				// Debug.Log(ToString());
			}
		}

		protected abstract List<AgentState<G, A>> GetStates();
		
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

		public override string ToString()
		{
			var states = new List<string>();
			var transitions = new List<string>();

			var rootStates = States.Where(
				possibleChild => States.None(s => s.ChildStates.Contains(possibleChild))
			).ToList();
			
			var stateNameMap = new Dictionary<AgentState<G, A>, string>();
			
			foreach (var state in rootStates)
			{
				states.Add(state.Name);
				stateNameMap.Add(state, state.Name);

				List<string> getChildStateNames(string path, List<AgentState<G, A>> childStates)
				{
					var childResults = new List<string>();

					foreach (var currChild in childStates)
					{
						var currChildPath = path + "." + currChild.Name;
						childResults.Add(currChildPath);
						stateNameMap.Add(currChild, currChildPath);
						childResults.AddRange(getChildStateNames(currChildPath, currChild.ChildStates));
					}

					return childResults;
				}
				
				states.AddRange(
					getChildStateNames(
						state.Name,
						state.ChildStates
					)	
				);
			}

			foreach (var state in States)
			{
				var sourceStateName = stateNameMap[state];
				foreach (var transition in state.Transitions)
				{
					var destinationStateName = stateNameMap[States.FirstOrDefault(s => s.GetType() == transition.TargetState)];
					transitions.Add(
						"\"" + sourceStateName + "\" -> \"" + destinationStateName + "\" [ label = \"" + transition.Name + "\" ]"
					);
				}
			}
			
			var result = "digraph " + GetType().Name + " {";

			foreach (var state in states)
			{
				result += "\n\t \"" + state + "\" [ shape = circle, label = \"" + state.Split('.').LastOrFallback(state) + "\"]";
			}

			foreach (var transition in transitions) result += "\n\t" + transition;

			result += "\n}";
			
			return result;
		}
	}
}