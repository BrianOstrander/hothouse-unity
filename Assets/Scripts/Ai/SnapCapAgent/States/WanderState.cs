using System;
using Lunra.Hothouse.Models;
using UnityEngine;

namespace Lunra.Hothouse.Ai.SnapCap
{
	public class WanderState<S> : AgentState<GameModel, SnapCapModel>
		where S : AgentState<GameModel, SnapCapModel>
	{
		public class Configuration
		{
			public Func<int> GetTimeoutLimit;
			public Func<float> GetDistance;
			public Func<Quaternion> GetRotation;
			public Func<DayTime> GetForbiddenExpiration;
			
			public Configuration( 
				Func<int> getTimeoutLimit,
				Func<float> getDistance,
				Func<Quaternion> getRotation,
				Func<DayTime> getForbiddenExpiration
			)
			{
				GetTimeoutLimit = getTimeoutLimit;
				GetDistance = getDistance;
				GetRotation = getRotation;
				GetForbiddenExpiration = getForbiddenExpiration;
			}
		}
		
		int timeouts;
		int timeoutLimit;
		DayTime forbiddenExpires;
		
		Configuration currentConfiguration;

		public WanderState<S> Configure(Configuration configuration)
		{
			currentConfiguration = configuration;
			return this;
		}
		
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

		public override void Begin()
		{
			if (currentConfiguration == null) Debug.LogError(nameof(currentConfiguration) + " has never been configured");
		}

		public override void Idle() => timeouts++;

		public override void End() => timeouts++;

		public class ToWander : AgentTransition<S, WanderState<S>, GameModel, SnapCapModel>
		{
			public override bool IsTriggered() => TargetState.forbiddenExpires < Game.SimulationTime.Value;

			public override void Transition()
			{
				TargetState.timeouts = 0;
				TargetState.timeoutLimit = TargetState.currentConfiguration.GetTimeoutLimit();
			}
		}

		class ToReturnOnTimeout : AgentTransition<WanderState<S>, S, GameModel, SnapCapModel>
		{
			public override bool IsTriggered() => SourceState.timeoutLimit <= SourceState.timeouts;

			public override void Transition()
			{
				SourceState.forbiddenExpires = SourceState.currentConfiguration.GetForbiddenExpiration();
			}
		}

		class ToNavigateRandomDirection : AgentTransition<WanderState<S>, NavigateState, GameModel, SnapCapModel>
		{
			Navigation.Result navigationResult;
			
			public override bool IsTriggered()
			{
				var nearestFloorFound = NavigationUtility.CalculateNearestFloor(
					Agent.Transform.Position.Value + ((SourceState.currentConfiguration.GetRotation() * Vector3.forward) * SourceState.currentConfiguration.GetDistance()),
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
		
		protected class NavigateState : BaseNavigateState<WanderState<S>, SnapCapModel> { }
	}
}