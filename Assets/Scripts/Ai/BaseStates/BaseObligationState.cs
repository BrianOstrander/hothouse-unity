using Lunra.Hothouse.Models;
using UnityEngine;

namespace Lunra.Hothouse.Ai
{
	public abstract class BaseObligationState<S0, S1, A> : AgentState<GameModel, A>
		where S0 : AgentState<GameModel, A>
		where S1 : BaseObligationState<S0, S1, A>
		where A : AgentModel
	{
		struct Cache
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
		
		public override string Name => "Obligation";

		BaseTimeoutState<S1, A> timeoutState;
		
		Cache cache = Cache.Default();
		
		int timeouts;
		int timeoutsLimit = 1;

		public override void OnInitialize()
		{
			AddChildStates(
				new BaseNavigateState<S1, A>(),
				timeoutState = new BaseTimeoutState<S1, A>()
			);

			AddTransitions(
				new ToReturnOnMissingObligation(),
				new ToReturnOnTimeout(),
				new ToTimeoutOnTarget(),
				new ToNavigateToTarget()
			);
		}

		public override void Begin() => Recalculate();

		public override void Idle()
		{
			Recalculate();
			timeouts++;
		}

		void Recalculate()
		{
			cache = Cache.Default();
			
			if (Agent.ObligationPromises.All.TryPeek(out cache.CurrentObligation))
			{
				cache.CurrentObligation.Target.TryGetInstance(
					Game,
					out cache.TargetParent
				);
				cache.IsTargetNull = cache.TargetParent == null;
		
				if (!cache.IsTargetNull)
				{
					cache.Target = cache.TargetParent.Obligations;
		
					switch (cache.TargetParent)
					{
						case IEnterableModel targetParentEnterable:
							cache.IsNavigable = NavigationUtility.CalculateNearest(
								Agent.Transform.Position.Value,
								out cache.NavigationResult,
								Navigation.QueryEntrances(targetParentEnterable)
							);
							cache.NavigationRadiusMaximum = 0.1f; // TODO: Don't hardcode this
							break;
						default:
							Debug.LogError("Unrecognized target parent type: "+cache.Target.GetType().Name);
							break;
					}
				}
			}
		}

		public class ToObligationOnExistingObligations : AgentTransition<S0, S1, GameModel, A>
		{
			public override bool IsTriggered()
			{
				return Agent.ObligationPromises.All.TryPeek(out _);
			}
		}
		
		class ToReturnOnMissingObligation : AgentTransition<S1, S0, GameModel, A>
		{
			public override bool IsTriggered() => SourceState.cache.IsTargetNull;
		}
		
		class ToTimeoutOnTarget : AgentTransition<S1, BaseTimeoutState<S1, A>, GameModel, A>
		{
			public override string Name => "ToTimeoutOnTarget";

			public override bool IsTriggered()
			{
				if (!SourceState.cache.IsNavigable) return false;
				if (SourceState.cache.NavigationRadiusMaximum < Vector3.Distance(Agent.Transform.Position.Value, SourceState.cache.NavigationResult.Target)) return false;

				return true;
			}

			public override void Transition()
			{
				Agent.ObligationPromises.All.Pop();

				SourceState.cache.Target.Trigger(SourceState.cache.CurrentObligation.Obligation, Agent);
				
				SourceState.timeoutState.ConfigureForInterval(Interval.WithMaximum(1f)); // TODO: Don't hardcode this...
			}
		}

		class ToNavigateToTarget : AgentTransition<S1, BaseNavigateState<S1, A>, GameModel, A>
		{
			public override string Name => "ToNavigateToTarget";
			
			public override bool IsTriggered() => SourceState.cache.IsNavigable;

			public override void Transition() => Agent.NavigationPlan.Value = NavigationPlan.Navigating(SourceState.cache.NavigationResult.Path);
		}

		class ToReturnOnTimeout : AgentTransition<S1, S0, GameModel, A>
		{
			public override string Name => "ToReturnOnTimeout";
			
			public override bool IsTriggered() => SourceState.timeoutsLimit < SourceState.timeouts;

			public override void Transition()
			{
				SourceState.timeouts = 0;
				Agent.ObligationPromises.All.Pop();
			}
		}
	}
}