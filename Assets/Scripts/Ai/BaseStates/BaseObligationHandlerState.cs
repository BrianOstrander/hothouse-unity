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
							
							CurrentCache.NavigationRadiusMaximum = CalculateInteractionRadius(
								CurrentCache.TargetParent,
								CurrentCache.NavigationResult
							);
							break;
						default:
							Debug.LogError("Unrecognized target parent type: "+CurrentCache.Target.GetType().Name);
							break;
					}
				}
			}
		}

		protected virtual float CalculateInteractionRadius(IObligationModel targetParent, Navigation.Result navigationResult) => Agent.InteractionRadius.Value;

		public class ToObligationOnExistingObligation : AgentTransition<S0, S1, GameModel, A>
		{
			public override bool IsTriggered()
			{
				return Agent.ObligationPromises.All.TryPeek(out var obligation) && TargetState.ObligationsHandled.Any(o => o.Type == obligation.Obligation.Type);
			}
		}
		
		public class ToObligationHandlerOnAvailableObligation : AgentTransition<S0, S1, GameModel, A>
		{
			List<(IObligationModel Parent, Obligation Obligation)> entries = new List<(IObligationModel Parent, Obligation Obligation)>();
			(IObligationModel Parent, Obligation Obligation) selection;
			
			public override bool IsTriggered()
			{
				if (!Game.Cache.Value.AnyObligationsAvailable) return false;
				if (TargetState.ObligationsHandled.None(o => Game.Cache.Value.UniqueObligationsAvailable.Contains(o.Type))) return false;
				
				entries.Clear();
				selection = default;

				foreach (var obligationParent in Game.Query.All<IObligationModel>())
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
					if (!IsObligationParentValid(obligationParent)) continue;
					var obligation = obligationParent.Obligations.All.Value.Available
						.FirstOrDefault(
							o => TargetState.ObligationsHandled.Any(h => h.Type == o.Type) && IsObligationValid(obligationParent, o)
						);
					if (obligation == null) continue;

					entries.Add((obligationParent, obligation));
				}

				if (entries.None()) return false;

				foreach (var entry in entries.OrderBy(e => Vector3.Distance(Agent.Transform.Position.Value, e.Parent.Transform.Position.Value)))
				{
					if (!Navigation.TryQuery(entry.Parent, out var query)) continue;

					var isNavigable = NavigationUtility.CalculateNearest(
						Agent.Transform.Position.Value,
						out _,
						query
					);
					
					if (!isNavigable) continue;

					selection = entry;
					break;
				}

				return selection.Obligation != null;
			}

			public override void Transition()
			{
				selection.Parent.Obligations.AddForbidden(selection.Obligation);
				Agent.ObligationPromises.All.Push(
					ObligationPromise.New(
						selection.Obligation,
						selection.Parent
					)	
				);
			}

			protected virtual bool IsObligationParentValid(IObligationModel obligationParent) => true;
			protected virtual bool IsObligationValid(IObligationModel obligationParent, Obligation obligation) => true;
		}

		protected class ToReturnOnMissingObligation : AgentTransition<S1, S0, GameModel, A>
		{
			public override bool IsTriggered() => SourceState.CurrentCache.IsTargetNull;
		}
		
		protected class ToNavigateToTarget : AgentTransition<S1, NavigateState, GameModel, A>
		{
			public override string Name => "ToNavigateToTarget";
			
			public override bool IsTriggered() => SourceState.CurrentCache.IsNavigable;

			public override void Transition()
			{
				GetNavigationInterrupts(
					out var interrupt,
					out var radiusThreshold,
					out var pathThreshold
				);
				
				Agent.NavigationPlan.Value = NavigationPlan.Navigating(
					SourceState.CurrentCache.NavigationResult.Path,
					interrupt,
					radiusThreshold,
					pathThreshold
				);
			}

			public virtual void GetNavigationInterrupts(
				out NavigationPlan.Interrupts interrupt,
				out float radiusThreshold,
				out float pathThreshold
			)
			{
				interrupt = NavigationPlan.Interrupts.RadiusThreshold;
				radiusThreshold = Agent.InteractionRadius.Value;
				pathThreshold = 0f;
			}
		}

		protected class ToReturnOnTimeout : AgentTransition<S1, S0, GameModel, A>
		{
			public override string Name => "ToReturnOnTimeout";
			
			public override bool IsTriggered() => SourceState.timeoutsLimit < SourceState.timeouts;

			public override void Transition()
			{
				SourceState.timeouts = 0;
				
				Agent.ObligationPromises.BreakPromise();
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
			protected bool HasPoppedObligation { get; private set; }
			protected virtual DayTime TimeoutDuration => DayTime.FromHours(1f);

			public override void Transition()
			{
				HasPoppedObligation = false;
				
				OnTimeoutBegin();
				
				SourceState.TimeoutInstance.ConfigureForInterval(
					TimeoutDuration,
					delta =>
					{
						if (delta.IsDone)
						{
							OnTimeoutEnd();
							TryPopObligation();
						}
						else OnTimeoutUpdate(delta.Progress);
					}
				);
			}

			protected virtual void OnTimeoutBegin() { }

			protected virtual void OnTimeoutUpdate(float progress) { }

			protected virtual void OnTimeoutEnd() { }

			protected virtual bool TryPopObligation()
			{
				if (HasPoppedObligation) return true;
				if (!CanPopObligation) return false;

				Agent.ObligationPromises.All.Pop();
				SourceState.CurrentCache.Target.Trigger(SourceState.CurrentCache.CurrentObligation.Obligation, Agent);
				Agent.ObligationPromises.Complete(SourceState.CurrentCache.CurrentObligation.Obligation);

				return HasPoppedObligation = true;
			}
		}
		
		#region Child States
		protected class NavigateState : BaseNavigateState<S1, A> { }
		protected class TimeoutState : BaseTimeoutState<S1, A> { }
		#endregion
	}
}