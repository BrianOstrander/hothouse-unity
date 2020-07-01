using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using UnityEngine;

namespace Lunra.Hothouse.Ai
{
	public interface IAgentStateMachine
	{
		string Name { get; }
		string GetSerializedGraph(bool highlightCurrentState = false);
	}

	public abstract class AgentStateMachine<G, A> : IAgentStateMachine
		where A : AgentModel
	{
		public string Name => Agent.ShortId + "<" + GetType().Name + ">";
		
		public List<AgentState<G, A>> States { get; } = new List<AgentState<G, A>>();
		
		public AgentState<G, A> DefaultState { get; protected set; }
		public AgentState<G, A> CurrentState { get; private set; }
		
		public G Game { get; private set; }
		public A Agent { get; private set; }

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
				
				state.Initialize(this);
				
				statesRemainingToInitialize.AddRange(state.ChildStates);
			}

			foreach (var state in States) state.InitializeTransitions();

			if (DefaultState == null)
			{
				Debug.LogError("No "+nameof(DefaultState)+" specified for "+GetType().Name);
				return;
			}

			CurrentState = DefaultState;

			if (agent.IsDebugging)
			{
				Debug.Log(Name + ": entering default state " + CurrentState.Name);
				Debug.Log(ToString());
			}
		}

		protected abstract List<AgentState<G, A>> GetStates();

		public bool TryGetState<S>(
			out S state
		)
			where S : AgentState<G, A>
		{
			state = States.FirstOrDefault(s => s is S) as S;
			return state != null;
		}
		
		public void Update()
		{
			foreach (var transition in CurrentState.Transitions)
			{
				if (!transition.IsTriggered()) continue;

				var targetState = transition.TransitionTargetState;
				
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

		public override string ToString() => GetSerializedGraph();
		
		public string GetSerializedGraph(bool highlightCurrentState = false)
		{
			var transitions = new List<string>();

			var rootStates = States.Where(
				possibleChild => States.None(s => s.ChildStates.Contains(possibleChild))
			).ToList();
			
			var stateMap = new Dictionary<AgentState<G, A>, (string Name, Color Color)>();

			void addStateMapEntry(
				AgentState<G, A> state,
				string name
			)
			{
				stateMap.Add(
					state,
					(
						name,
						name.ToColor()
					)
				);
			}
			
			foreach (var state in rootStates)
			{
				addStateMapEntry(state, state.Name);

				void getChildStateNames(string path, List<AgentState<G, A>> childStates)
				{
					foreach (var currChild in childStates)
					{
						var currChildPath = path + "." + currChild.Name;
						addStateMapEntry(currChild, currChildPath);
						getChildStateNames(currChildPath, currChild.ChildStates);
					}
				}

				getChildStateNames(
					state.Name,
					state.ChildStates
				);
			}

			foreach (var state in States)
			{
				var sourceState = stateMap[state];
				foreach (var transition in state.Transitions)
				{
					var targetState = stateMap[States.FirstOrDefault(s => s.GetType() == transition.TransitionTargetState.GetType())];
					var transitionResult = "\"" + sourceState.Name + "\" -> \"" + targetState.Name + "\"";
					transitionResult += " [ ";
					transitionResult += "label = \"" + transition.Name + "\"";
					transitionResult += ", color = \"#" + sourceState.Color.ToHtmlRgb() + "\"";
					transitionResult += ", fontcolor = \"#" + sourceState.Color.NewV(0.65f).ToHtmlRgb() + "\"";
					transitionResult += " ]";
					
					transitions.Add(
						transitionResult
					);
				}
			}
			
			var result = "digraph " + GetType().Name + " {";

			var defaultFillSaturation = highlightCurrentState ? 0.08f : 0.16f;
			
			foreach (var kv in stateMap)
			{
				var state = kv.Key;
				var entry = kv.Value;

				var fillSaturation = defaultFillSaturation;
				if (highlightCurrentState && state == CurrentState) fillSaturation = 0.75f;
				
				result += "\n\t\"" + entry.Name + "\"";
				result += " [ ";
				result += "shape = circle";
				result += ", label = \"" + entry.Name.Split('.').LastOrFallback(entry.Name) + "\"";
				result += ", color = \"#" + entry.Color.ToHtmlRgb() + "\"";
				result += ", fillcolor = \"#" + entry.Color.NewS(fillSaturation).ToHtmlRgb() + "\"";
				result += ", style = \"filled\"";
				result += " ]";
				
				// result += "\n\t \"" + state + "\" [ shape = circle, label = \"" + state.Split('.').LastOrFallback(state) + "\"]";
			}

			foreach (var transition in transitions) result += "\n\t" + transition;

			result += "\n}";
			
			return result;
		}
	}
}