using Lunra.Hothouse.Models;
using Lunra.StyxMvp.Models;
using UnityEngine;

namespace Lunra.Hothouse.Ai
{
	public abstract class BaseInventoryRequestState<S0, S1, A> : AgentState<GameModel, A>
		where S0 : AgentState<GameModel, A>
		where S1 : BaseInventoryRequestState<S0, S1, A>
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
		
			public InventoryTransaction Transaction;
			public BaseInventoryComponent Target;

			public bool IsTargetNull;
			public IModel TargetParent;
			public Navigation.Result NavigationResult;
			public bool IsNavigable;
			public float NavigationRadiusMaximum;
		}
	
		public override string Name => "InventoryRequest";

		BaseTimeoutState<S1, A> timeoutState;
		
		Cache cache = Cache.Default();
		int timeouts;

		public override void OnInitialize()
		{
			AddChildStates(
				new BaseNavigateState<S1, A>(),
				timeoutState = new BaseTimeoutState<S1, A>()
			);
			
			AddTransitions(
				new ToReturnOnMissingTransaction(),
				new ToReturnOnTimeout(),
				
				new ToTimeoutOnDeliverTarget(),
				new ToNavigateToDeliverTarget()
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
			
			if (Agent.InventoryPromises.Transactions.TryPeek(out cache.Transaction))
			{
				cache.Transaction.Target.TryGetInstance(
					Game,
					out cache.Target
				);
				cache.IsTargetNull = cache.Target == null;

				if (!cache.IsTargetNull)
				{
					cache.TargetParent = Game.GetInventoryParent(cache.Target.Id.Value);

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

		public class ToInventoryRequestOnPromises : AgentTransition<S0, S1, GameModel, A>
		{
			public override bool IsTriggered()
			{
				return Agent.InventoryPromises.Transactions.TryPeek(out _);
			}
		}
		
		class ToReturnOnMissingTransaction : AgentTransition<S1, S0, GameModel, A>
		{
			public override bool IsTriggered() => SourceState.cache.IsTargetNull;
		}
		
		class ToTimeoutOnDeliverTarget : AgentTransition<S1, BaseTimeoutState<S1, A>, GameModel, A>
		{
			public override bool IsTriggered()
			{
				if (!SourceState.cache.IsNavigable) return false;
				if (SourceState.cache.Transaction.Type != InventoryTransaction.Types.Deliver) return false;

				return Vector3.Distance(Agent.Transform.Position.Value, SourceState.cache.NavigationResult.Target) < SourceState.cache.NavigationRadiusMaximum;
			}

			public override void Transition()
			{
				SourceState.timeoutState.ConfigureForInterval(Interval.WithMaximum(1f));
			}
		}

		class ToNavigateToDeliverTarget : AgentTransition<S1, BaseNavigateState<S1, A>, GameModel, A>
		{
			public override bool IsTriggered()
			{
				if (SourceState.cache.Transaction.Type != InventoryTransaction.Types.Deliver) return false;
				Debug.Log("uh: "+SourceState.cache.IsNavigable);
				return SourceState.cache.IsNavigable;
			}

			public override void Transition() => Agent.NavigationPlan.Value = NavigationPlan.Navigating(SourceState.cache.NavigationResult.Path);
		}

		class ToReturnOnTimeout : AgentTransition<S1, S0, GameModel, A>
		{
			public override bool IsTriggered() => 1 < SourceState.timeouts;

			public override void Transition()
			{
				Agent.InventoryPromises.Transactions.Pop();
				
				switch (SourceState.cache.Target)
				{
					case InventoryComponent inventory:
						switch (SourceState.cache.Transaction.Type)
						{
							case InventoryTransaction.Types.Deliver:
								inventory.RemoveReserved(SourceState.cache.Transaction.Items);
								break;
							case InventoryTransaction.Types.Distribute:
								inventory.RemoveForbidden(SourceState.cache.Transaction.Items);
								break;
							default:
								Debug.LogError("Unrecognized Type: "+SourceState.cache.Transaction.Type);
								break;
						}
						break;
					default:
						Debug.LogError("Unrecognized inventory type: "+SourceState.cache.Target.GetType().Name);
						break;
				}
			}
		}
		
		
	}
}