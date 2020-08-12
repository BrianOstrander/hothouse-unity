using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using UnityEngine;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class SatiateGoalsState<S> : AgentState<GameModel, DwellerModel>
		where S : AgentState<GameModel, DwellerModel>
	{
		const int IdleCountMaximum = 10;
		
		public override string Name => "SatiateGoals";

		TimeoutState timeoutState;
		int idleCount;
		
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

		public override void Begin()
		{
			idleCount = 0;

			if (Agent.Bed.Value.TryGetInstance<IClaimOwnershipModel>(Game, out var bed) && bed.Ownership.Contains(Agent)) return;
			
			Agent.Bed.Value = InstanceId.Null();

			foreach (var possibleBed in Game.Buildings.AllActive.Where(m => m.Tags.Contains(BuildingTags.Bed)))
			{
				if (possibleBed.Ownership.IsFull) continue;

				var isNavigable = NavigationUtility.CalculateNearest(
					Agent.Transform.Position.Value,
					out _,
					Navigation.QueryEntrances(possibleBed)
				);

				if (isNavigable)
				{
					possibleBed.Ownership.Add(Agent);
					Agent.Bed.Value = possibleBed.GetInstanceId();
					return;
				}
			}
		}

		public override void Idle()
		{
			if (Agent.JobShift.Value.Contains(Game.SimulationTime.Value)) idleCount++;
		}

		public class ToSatiateGoalsOnShiftEnd : AgentTransition<S, SatiateGoalsState<S>, GameModel, DwellerModel>
		{
			public override bool IsTriggered() => !Agent.JobShift.Value.Contains(Game.SimulationTime.Value);
		}

		class ToReturnOnShiftBegin : AgentTransition<SatiateGoalsState<S>, S, GameModel, DwellerModel>
		{
			public override bool IsTriggered()
			{
				if (!Agent.JobShift.Value.Contains(Game.SimulationTime.Value)) return false;

				return !Agent.Goals.AnyAtMaximum || IdleCountMaximum <= SourceState.idleCount;
			}
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

				if (destination.Enterable.Entrances.Value.None(e => e.State == Entrance.States.Available && Vector3.Distance(e.Position, Agent.Transform.Position.Value) < Agent.InteractionRadius.Value))
				{
					Debug.LogError("Destination found but no entrances available, this is unexpected");
					return false;
				}

				return true;
			}

			public override void Transition()
			{
				var activity = destination.Activities.GetActivity(reservation.ReservationId);
				var previousProgress = 0f;
				
				SourceState.timeoutState.ConfigureForDayAndTime(
					reservation.AppointmentEnd,
					delta =>
					{
						var progressDelta = Mathf.Max(0f, delta.Progress - previousProgress);

						Agent.Goals.Apply(
							progressDelta,
							activity.Modifiers
						);
						
						previousProgress = delta.Progress;

						if (delta.IsDone)
						{
							// TODO: Need to check if building has been destroyed...
							destination.Activities.UnReserveActivity(Agent);
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
					
					var activityReservationBegin = Game.SimulationTime.Value + deltaTime;

					var availableActivities = activityParent.Activities.GetAvailable(activityReservationBegin);
					
					if (availableActivities.None()) continue;

					foreach (var activity in availableActivities)
					{
						if (activity.RequiresOwnership)
						{
							if (activityParent is IClaimOwnershipModel activityParentClaimOwnershipModel)
							{
								if (!activityParentClaimOwnershipModel.Ownership.Contains(Agent)) continue;
							}
							else Debug.LogWarning($"Activity {activity.Id} on {activityParent.ShortId} requires ownership but parent of type {activityParent.GetType().Name} does not implement {nameof(IClaimOwnershipModel)}");
						}
						
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
					bestActivity,
					bestActivityReservationBegin
				);
				
				Agent.NavigationPlan.Value = NavigationPlan.Navigating(
					bestNavigationResult.Path,
					NavigationPlan.Interrupts.RadiusThreshold,
					Agent.InteractionRadius.Value
				);
			}
		}
			
		protected class TimeoutState : TimeoutState<SatiateGoalsState<S>> { }
		protected class NavigateState : NavigateState<SatiateGoalsState<S>> { }
	}
}