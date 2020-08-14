using Lunra.Hothouse.Models;
using Lunra.NumberDemon;
using UnityEngine;

namespace Lunra.Hothouse.Ai.Bubbler
{
	public class WanderState<S> : AgentState<GameModel, BubblerModel>
		where S : AgentState<GameModel, BubblerModel>
	{
		Demon generator = new Demon();
		int timeouts;
		int timeoutLimit;
		DayTime forbiddenExpires;
		
		public override void OnInitialize()
		{
			AddChildStates(
				new NavigateState()
			);
			
			AddTransitions(
				new ToReturnOnTimeout(),
				
				new ToNavigateRandomDirection()
			);
		}

		public override void Idle() => timeouts++;

		public override void End() => timeouts++;

		public class ToWander : AgentTransition<S, WanderState<S>, GameModel, BubblerModel>
		{
			public override bool IsTriggered() => TargetState.forbiddenExpires < Game.SimulationTime.Value;

			public override void Transition()
			{
				TargetState.timeouts = 0;
				TargetState.timeoutLimit = TargetState.generator.GetNextInteger(3, 6);
			}
		}

		class ToReturnOnTimeout : AgentTransition<WanderState<S>, S, GameModel, BubblerModel>
		{
			public override bool IsTriggered() => SourceState.timeoutLimit <= SourceState.timeouts;

			public override void Transition()
			{
				SourceState.forbiddenExpires = Game.SimulationTime.Value + DayTime.FromRealSeconds(SourceState.generator.GetNextFloat(3f, 10f));
			}
		}

		class ToNavigateRandomDirection : AgentTransition<WanderState<S>, NavigateState, GameModel, BubblerModel>
		{
			Navigation.Result navigationResult;
			
			public override bool IsTriggered()
			{
				var nearestFloorFound = NavigationUtility.CalculateNearestFloor(
					Agent.Transform.Position.Value + ((SourceState.generator.GetNextRotation() * Vector3.forward) * SourceState.generator.GetNextFloat(1f, 4f)),
					out var navHit,
					out _,
					out _
				);

				if (!nearestFloorFound) return false;

				var isNavigable = NavigationUtility.CalculateNearest(
					Agent.Transform.Position.Value,
					out navigationResult,
					Navigation.QueryPosition(navHit.position)
				);

				return isNavigable;
			}

			public override void Transition() => Agent.NavigationPlan.Value = NavigationPlan.Navigating(navigationResult.Path);
		}
		protected class NavigateState : NavigateState<WanderState<S>> { }
	}
}