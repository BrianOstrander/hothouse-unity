using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using UnityEngine;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class SatiateGoalsState<S> : AgentState<GameModel, DwellerModel>
		where S : AgentState<GameModel, DwellerModel>
	{
		public override string Name => "SatiateGoals";

		TimeoutState timeoutState;
		
		public override void OnInitialize()
		{
			AddChildStates(
				new NavigateState(),
				timeoutState = new TimeoutState()
			);
			
			AddTransitions(
				new ToTimeoutOnActivity(),
				new ToNavigateToActivity(),
				new ToReturnOnShiftBegin()	
			);
		}
		
		public class ToSatiateGoalsOnShiftEnd : AgentTransition<S, SatiateGoalsState<S>, GameModel, DwellerModel>
		{
			public override bool IsTriggered() => !Agent.JobShift.Value.Contains(Game.SimulationTime.Value);
		}

		class ToReturnOnShiftBegin : AgentTransition<SatiateGoalsState<S>, S, GameModel, DwellerModel>
		{
			public override bool IsTriggered() => Agent.JobShift.Value.Contains(Game.SimulationTime.Value);
		}

		class ToTimeoutOnActivity : AgentTransition<SatiateGoalsState<S>, TimeoutState, GameModel, DwellerModel>
		{
			GoalActivityReservation reservation;
			IGoalActivityModel destination;
			
			public override bool IsTriggered()
			{
				if (!Agent.GoalPromises.All.TryPeek(out reservation)) return false;
				if (!reservation.Destination.TryGetInstance(Game, out destination))
				{
					Debug.LogError("Found goal promise on stack, but could not get a destination. This is unexpected");
					return false;
				}

				if (destination.Enterable.Entrances.Value.None(e => e.State == Entrance.States.Available && Vector3.Distance(e.Position, Agent.Transform.Position.Value) < Agent.MeleeRange.Value))
				{
					Debug.LogError("Destination found but no entrances available, this is unexpected");
					return false;
				}

				return true;
			}

			public override void Transition()
			{
				SourceState.timeoutState.ConfigureForDayAndTime(
					reservation.AppointmentEnd,
					delta =>
					{
						if (delta.IsDone)
						{
							// TODO: Need to check if building has been destroyed...
							destination.Activities.UnReserveActivity(
								Agent,
								destination
							);
						}
					}
				);
			}
		}

		class ToNavigateToActivity : AgentTransition<SatiateGoalsState<S>, NavigateState, GameModel, DwellerModel>
		{
			Navigation.Result bestNavigationResult;
			GoalActivity bestActivity;
			IGoalActivityModel bestActivityParent;
			DayTime bestActivityReservationBegin;
			
			public override bool IsTriggered()
			{
				var minimumDiscontent = float.MaxValue;
				var minimumDiscontentDelta = 0f;
				
				foreach (var activityParent in Game.GetActivities())
				{
					if (!activityParent.Enterable.AnyAvailable()) continue;

					var isNavigable = NavigationUtility.CalculateNearest(
						Agent.Transform.Position.Value,
						out var navigationResult,
						Navigation.QueryEntrances(activityParent)
					);
					
					if (!isNavigable) continue;
					
					var deltaTime = navigationResult.CalculateNavigationTime(Agent.NavigationVelocity.Value);
					
					var activityReservationBegin = Game.SimulationTime.Value + new DayTime(deltaTime);

					var availableActivities = activityParent.Activities.GetAvailable(activityReservationBegin);
					
					if (availableActivities.None()) continue;

					foreach (var activity in availableActivities)
					{
						var hasLessDiscontent = Agent.Goals.TryCalculateDiscontent(
							activity,
							deltaTime,
							minimumDiscontent,
							out var improvedDiscontent
						);
						
						if (!hasLessDiscontent) continue;

						minimumDiscontent = improvedDiscontent;
						bestNavigationResult = navigationResult;
						bestActivity = activity;
						bestActivityParent = activityParent;
						bestActivityReservationBegin = activityReservationBegin;
					}
				}

				return !Mathf.Approximately(minimumDiscontent, float.MaxValue);
			}

			public override void Transition()
			{
				bestActivityParent.Activities.ReserveActivity(
					Agent,
					bestActivityParent,
					bestActivity,
					bestActivityReservationBegin
				);
				
				Agent.NavigationPlan.Value = NavigationPlan.Navigating(bestNavigationResult.Path);
			}
		}
			
		protected class TimeoutState : TimeoutState<SatiateGoalsState<S>> { }
		protected class NavigateState : NavigateState<SatiateGoalsState<S>> { }
	}
}