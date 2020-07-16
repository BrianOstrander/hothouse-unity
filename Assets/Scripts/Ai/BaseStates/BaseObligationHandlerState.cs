using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using UnityEngine;

namespace Lunra.Hothouse.Ai
{
	public abstract class BaseObligationHandlerState<S0, S1, A> : AgentState<GameModel, A>
		where S0 : AgentState<GameModel, A>
		where S1 : BaseObligationHandlerState<S0, S1, A>
		where A : AgentModel
	{
		protected struct Cache
		{
			public static Cache Default()
			{
				var result = new Cache();
				result.IsTargetNull = true;
				return result;
			}
		
			// TODO: Rename to promise...
			public ObligationPromise CurrentObligation;
			public ObligationComponent Target;

			public bool IsTargetNull;
			public IObligationModel TargetParent;
			public Navigation.Result NavigationResult;
			public bool IsNavigable;
			public float NavigationRadiusMaximum;
		}
		
		public abstract Obligation[] ObligationsHandled { get; }

		protected virtual bool RequiresOwnership => false;

		protected TimeoutState TimeoutInstance { get; set; } 
			
		protected Cache CurrentCache = Cache.Default();
		
		int timeouts;
		int timeoutsLimit = 1;

		public override void Begin() => Recalculate();

		public override void Idle()
		{
			Recalculate();
			timeouts++;
		}

		void Recalculate()
		{
			CurrentCache = Cache.Default();
			
			if (Agent.ObligationPromises.All.TryPeek(out CurrentCache.CurrentObligation))
			{
				CurrentCache.CurrentObligation.Target.TryGetInstance(
					Game,
					out CurrentCache.TargetParent
				);
				CurrentCache.IsTargetNull = CurrentCache.TargetParent == null;
		
				if (!CurrentCache.IsTargetNull)
				{
					CurrentCache.Target = CurrentCache.TargetParent.Obligations;
		
					switch (CurrentCache.TargetParent)
					{
						case IEnterableModel targetParentEnterable:
							CurrentCache.IsNavigable = NavigationUtility.CalculateNearest(
								Agent.Transform.Position.Value,
								out CurrentCache.NavigationResult,
								Navigation.QueryEntrances(targetParentEnterable)
							);
							CurrentCache.NavigationRadiusMaximum = 0.1f; // TODO: Don't hardcode this
							break;
						default:
							Debug.LogError("Unrecognized target parent type: "+CurrentCache.Target.GetType().Name);
							break;
					}
				}
			}
		}

		public class ToObligationOnExistingObligation : AgentTransition<S0, S1, GameModel, A>
		{
			public override bool IsTriggered()
			{
				return Agent.ObligationPromises.All.TryPeek(out var obligation) && TargetState.ObligationsHandled.Any(o => o.Type == obligation.Obligation.Type);
			}
		}
		
		public class ToObligationHandlerOnAvailableObligation : AgentTransition<S0, S1, GameModel, A>
		{
			List<IObligationModel> obligationParents = new List<IObligationModel>();
			IObligationModel selectedObligationParent;
			
			public override bool IsTriggered()
			{
				if (!Game.Cache.Value.AnyObligationsAvailable) return false;
				if (TargetState.ObligationsHandled.None(o => Game.Cache.Value.UniqueObligationsAvailable.Contains(o.Type))) return false;
				
				obligationParents.Clear();
				selectedObligationParent = null;

				foreach (var obligationParent in Game.GetObligations())
				{
					if (!obligationParent.Enterable.AnyAvailable()) continue;
					if (!obligationParent.Obligations.HasAny()) continue;
					if (TargetState.RequiresOwnership)
					{
						if (obligationParent is IClaimOwnershipModel obligationParentClaimable && !obligationParentClaimable.Ownership.Contains(Agent))
						{
							continue;
						}
					}
					if (obligationParent.Obligations.All.Value.Available.None(o => TargetState.ObligationsHandled.Any(h => h.Type == o.Type))) continue;
					
					obligationParents.Add(obligationParent);
				}

				if (obligationParents.None()) return false;

				foreach (var obligationParent in obligationParents.OrderBy(m => Vector3.Distance(Agent.Transform.Position.Value, m.Transform.Position.Value)))
				{
					if (!Navigation.TryQuery(obligationParent, out var query)) continue;

					var isNavigable = NavigationUtility.CalculateNearest(
						Agent.Transform.Position.Value,
						out _,
						query
					);
					
					if (!isNavigable) continue;

					selectedObligationParent = obligationParent;
					break;
				}

				return selectedObligationParent != null;
			}

			public override void Transition()
			{
				var obligation = selectedObligationParent.Obligations.All.Value.Available.First(o => TargetState.ObligationsHandled.Any(h => h.Type == o.Type));

				selectedObligationParent.Obligations.AddForbidden(obligation);
				Agent.ObligationPromises.All.Push(
					ObligationPromise.New(
						obligation,
						selectedObligationParent
					)	
				);
			}
		}

		protected class ToReturnOnMissingObligation : AgentTransition<S1, S0, GameModel, A>
		{
			public override bool IsTriggered() => SourceState.CurrentCache.IsTargetNull;
		}
		
		protected class ToNavigateToTarget : AgentTransition<S1, NavigateState, GameModel, A>
		{
			public override string Name => "ToNavigateToTarget";
			
			public override bool IsTriggered() => SourceState.CurrentCache.IsNavigable;

			public override void Transition() => Agent.NavigationPlan.Value = NavigationPlan.Navigating(SourceState.CurrentCache.NavigationResult.Path);
		}

		protected class ToReturnOnTimeout : AgentTransition<S1, S0, GameModel, A>
		{
			public override string Name => "ToReturnOnTimeout";
			
			public override bool IsTriggered() => SourceState.timeoutsLimit < SourceState.timeouts;

			public override void Transition()
			{
				SourceState.timeouts = 0;
				Debug.LogWarning("TODO: I think we need to break promises that weren't filled...");
				Agent.ObligationPromises.All.Pop();
			}
		}
		
		protected abstract class ToTimeoutOnTarget : AgentTransition<S1, BaseTimeoutState<S1, A>, GameModel, A>
		{
			public override string Name => "ToTimeoutOnTarget";

			public override bool IsTriggered()
			{
				if (!SourceState.CurrentCache.IsNavigable) return false;
				if (SourceState.CurrentCache.NavigationRadiusMaximum < Vector3.Distance(Agent.Transform.Position.Value, SourceState.CurrentCache.NavigationResult.Target)) return false;

				return true;
			}
			
			protected abstract bool CanPopObligation { get; }
			protected virtual float TimeoutDuration => 1f;

			public override void Transition()
			{
				OnTimeoutBegin();
				
				SourceState.TimeoutInstance.ConfigureForInterval(
					Interval.WithMaximum(TimeoutDuration),
					delta =>
					{
						if (delta.IsDone)
						{
							OnTimeoutEnd();
							if (CanPopObligation)
							{
								Agent.ObligationPromises.All.Pop();
								SourceState.CurrentCache.Target.Trigger(SourceState.CurrentCache.CurrentObligation.Obligation, Agent);
							}
						}
						else OnTimeoutUpdate(delta.Progress);
					}
				);
			}

			protected virtual void OnTimeoutBegin() { }

			protected virtual void OnTimeoutUpdate(float progress) { }

			protected virtual void OnTimeoutEnd() { }
		}
		
		#region Child States
		protected class NavigateState : BaseNavigateState<S1, A> { }
		protected class TimeoutState : BaseTimeoutState<S1, A> { }
		#endregion
	}
}