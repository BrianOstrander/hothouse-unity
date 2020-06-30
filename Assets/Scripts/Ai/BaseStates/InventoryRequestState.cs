using System;
using System.Linq;
using Lunra.Hothouse.Models;
using Lunra.StyxMvp.Models;
using UnityEngine;

namespace Lunra.Hothouse.Ai
{
	public class InventoryRequestState<S, A> : AgentState<GameModel, A>
		where S : AgentState<GameModel, A>
		where A : AgentModel
	{
		struct Cache
		{
			public static Cache Default()
			{
				var result = new Cache();
				result.isTargetNull = true;
				return result;
			}
		
			public InventoryTransaction transaction;
			public BaseInventoryComponent target;

			public bool isTargetNull;
			public IModel targetParent;
			public Navigation.Result navigationResult;
			public bool isNavigable;
			public float navigationRadiusMaximum;
		}
	
		public override string Name => "InventoryRequest";

		BaseTimeoutState<S, A> timeoutState;
		
		Cache cache = Cache.Default();
		int timeouts;

		public override void OnInitialize()
		{
			AddChildStates(
				new BaseNavigateState<S, A>(),
				timeoutState = new BaseTimeoutState<S, A>()
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
			
			if (Agent.InventoryPromises.Transactions.TryPeek(out cache.transaction))
			{
				cache.transaction.Target.TryGetInstance(
					Game,
					out cache.target
				);
				cache.isTargetNull = cache.target == null;

				if (!cache.isTargetNull)
				{
					cache.targetParent = Game.GetInventoryParent(cache.target.Id.Value);

					switch (cache.targetParent)
					{
						case IEnterableModel targetParentEnterable:
							cache.isNavigable = NavigationUtility.CalculateNearest(
								Agent.Transform.Position.Value,
								out cache.navigationResult,
								Navigation.QueryEntrances(targetParentEnterable)
							);
							cache.navigationRadiusMaximum = 0.1f; // TODO: Don't hardcode this
							

							break;
						default:
							Debug.LogError("Unrecognized target parent type: "+cache.target.GetType().Name);
							break;
					}
				}
			}
		}

		public class ToInventoryRequestOnPromises : AgentTransition<S, InventoryRequestState<S, A>, GameModel, A>
		{
			public override bool IsTriggered()
			{
				return Agent.InventoryPromises.Transactions.TryPeek(out _);
			}
		}
		
		class ToReturnOnMissingTransaction : AgentTransition<InventoryRequestState<S, A>, S, GameModel, A>
		{
			public override bool IsTriggered() => SourceState.cache.isTargetNull;
		}
		
		class ToTimeoutOnDeliverTarget : AgentTransition<InventoryRequestState<S, A>, BaseTimeoutState<S, A>, GameModel, A>
		{
			public override bool IsTriggered()
			{
				if (!SourceState.cache.isNavigable) return false;
				if (SourceState.cache.transaction.Type != InventoryTransaction.Types.Deliver) return false;

				return Vector3.Distance(Agent.Transform.Position.Value, SourceState.cache.navigationResult.Target) < SourceState.cache.navigationRadiusMaximum;
			}

			public override void Transition()
			{
				SourceState.timeoutState.ConfigureForInterval(Interval.WithMaximum(1f));
			}
		}

		class ToNavigateToDeliverTarget : AgentTransition<InventoryRequestState<S, A>, BaseNavigateState<S, A>, GameModel, A>
		{
			public override bool IsTriggered()
			{
				if (SourceState.cache.transaction.Type != InventoryTransaction.Types.Deliver) return false;
				Debug.Log("uh: "+SourceState.cache.isNavigable);
				return SourceState.cache.isNavigable;
			}

			public override void Transition() => Agent.NavigationPlan.Value = NavigationPlan.Navigating(SourceState.cache.navigationResult.Path);
		}

		class ToReturnOnTimeout : AgentTransition<InventoryRequestState<S, A>, S, GameModel, A>
		{
			public override bool IsTriggered() => 1 < SourceState.timeouts;

			public override void Transition()
			{
				Agent.InventoryPromises.Transactions.Pop();
				
				switch (SourceState.cache.target)
				{
					case InventoryComponent inventory:
						switch (SourceState.cache.transaction.Type)
						{
							case InventoryTransaction.Types.Deliver:
								inventory.RemoveReserved(SourceState.cache.transaction.Items);
								break;
							case InventoryTransaction.Types.Distribute:
								inventory.RemoveForbidden(SourceState.cache.transaction.Items);
								break;
							default:
								Debug.LogError("Unrecognized Type: "+SourceState.cache.transaction.Type);
								break;
						}
						break;
					default:
						Debug.LogError("Unrecognized inventory type: "+SourceState.cache.target.GetType().Name);
						break;
				}
			}
		}
		
		
	}
}