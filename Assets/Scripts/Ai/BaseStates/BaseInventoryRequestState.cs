using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
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
		int timeoutsLimit = 1;

		public override void OnInitialize()
		{
			AddChildStates(
				new BaseNavigateState<S1, A>(),
				timeoutState = new BaseTimeoutState<S1, A>()
			);

			AddTransitions(
				new ToReturnOnMissingTransaction()
			);

			var validTransactionTypes = EnumExtensions.GetValues(InventoryTransaction.Types.Unknown);
			
			foreach (var transactionType in validTransactionTypes)
			{
				AddTransitions(
					new ToReturnOnTimeout(transactionType)	
				);
			}
			
			foreach (var transactionType in validTransactionTypes)
			{
				AddTransitions(
					new ToTimeoutOnTarget(transactionType)	
				);
			}
			
			foreach (var transactionType in validTransactionTypes)
			{
				AddTransitions(
					new ToNavigateToTarget(transactionType)	
				);
			}
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
						case null:
							Debug.LogError("Unable to find parent for inventory " + cache.Target.ShortId + ", this should never happen");
							break;
						default:
							Debug.LogError("Unrecognized target parent type: "+cache.TargetParent.GetType().Name);
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
		
		class ToTimeoutOnTarget : AgentTransition<S1, BaseTimeoutState<S1, A>, GameModel, A>
		{
			public override string Name => "ToTimeoutOn" + transactionType + "Target";
			
			InventoryTransaction.Types transactionType;

			InventoryTransaction nextTransaction;
			bool pushNextTransaction;
			
			public ToTimeoutOnTarget(InventoryTransaction.Types transactionType) => this.transactionType = transactionType;
			
			public override bool IsTriggered()
			{
				if (!SourceState.cache.IsNavigable) return false;
				if (transactionType != SourceState.cache.Transaction.Type) return false;
				if (SourceState.cache.NavigationRadiusMaximum < Vector3.Distance(Agent.Transform.Position.Value, SourceState.cache.NavigationResult.Target)) return false;

				nextTransaction = null;
				pushNextTransaction = false;
				
				switch (transactionType)
				{
					case InventoryTransaction.Types.Deliver:
						if (!Agent.Inventory.All.Value.Contains(SourceState.cache.Transaction.Items))
						{
							Debug.LogError("Currently trying to deliver resources the agent does not have, this is unexpected");
							return false;
						}
						break;
					case InventoryTransaction.Types.Distribute:

						if (Agent.InventoryPromises.Transactions.TryPeek(out nextTransaction, 1))
						{
							if (nextTransaction.Type == InventoryTransaction.Types.Deliver) return true;
						}
						
						var isNextTargetNavigable = NavigationUtility.CalculateNearest(
							Agent.Transform.Position.Value,
							out var nextTargetNavigationResult,
							GetNavigationQueries(
								ValidateParentForDelivery,
								ValidateInventoryForDelivery
							)
						);

						if (isNextTargetNavigable)
						{
							GetBestInventoryForDelivery(
								nextTargetNavigationResult.TargetModel as IBaseInventoryModel, 
								out nextTransaction
							);
							pushNextTransaction = true;
						}
						else
						{
							Debug.LogError("TODO: Handle what happens when no target is available for distribution (drop on ground, etc)");
							return false;
						}
						break;
					default:
						Debug.LogError("Unrecognized Transaction.Type: " + SourceState.cache.Transaction.Type);
						break;
				}
				
				return true;
			}

			public override void Transition()
			{
				Agent.InventoryPromises.Transactions.Pop();

				switch (SourceState.cache.Target)
				{
					case InventoryComponent inventory:
						switch (SourceState.cache.Transaction.Type)
						{
							case InventoryTransaction.Types.Deliver:
								inventory.CompleteDeliver(SourceState.cache.Transaction);
								Agent.Inventory.Remove(SourceState.cache.Transaction.Items);
								break;
							case InventoryTransaction.Types.Distribute:
								inventory.CompleteDistribution(SourceState.cache.Transaction);

								if (!nextTransaction.Items.Contains(SourceState.cache.Transaction.Items))
								{
									Debug.LogError("TODO: Handle what happens when we can't distribute everything... this probably shouldn't happen?");
								}
								
								Agent.Inventory.Add(nextTransaction.Items);
								if (pushNextTransaction) Agent.InventoryPromises.Transactions.Push(nextTransaction);
								break;
							default:
								Debug.LogError("Unrecognized Transaction.Type: "+SourceState.cache.Transaction.Type);
								break;
						}
						break;
					default:
						Debug.LogError("Unrecognized Target: "+SourceState.cache.Target.GetType().Name);
						break;
				}

				SourceState.timeoutState.ConfigureForInterval(Interval.WithMaximum(1f)); // TODO: Don't hardcode this...
			}

			Navigation.Query[] GetNavigationQueries(
				Func<IBaseInventoryModel, bool> parentValidation = null,
				Func<IBaseInventoryComponent, bool> validation = null
			)
			{
				var results = new List<Navigation.Query>();
				
				foreach (var model in Game.GetInventoryParents())
				{
					if (!(parentValidation?.Invoke(model) ?? true)) continue; 
						
					foreach (var inventory in model.Inventories)
					{
						if (!(validation?.Invoke(inventory) ?? true)) continue;

						if (Navigation.TryQuery(model, out var query))
						{
							results.Add(query);
							break;
						}
					}
				}

				return results.ToArray();
			}

			bool ValidateParentForDelivery(IBaseInventoryModel parent)
			{
				return parent.Id.Value != Agent.Id.Value && parent.Id.Value != SourceState.cache.TargetParent.Id.Value;
			}
			
			bool ValidateInventoryForDelivery(IBaseInventoryComponent model)
			{
				if (model.Id.Value == SourceState.cache.Target.Id.Value) return false;
				
				bool hasCapacityFor(InventoryCapacity targetCapacity, Inventory targetInventory)
				{
					return targetCapacity.HasCapacityFor(targetInventory, SourceState.cache.Transaction.Items);
				}
				
				switch (model)
				{
					case InventoryComponent inventory:
						if (!inventory.Permission.Value.CanDeposit(Agent)) return false;
						return hasCapacityFor(inventory.AvailableCapacity.Value, inventory.Available.Value);
					case AgentInventoryComponent _:
						// For the moment, I'm going to forbid delivering to agents, because why do I need this?
						// return hasCapacityFor(agentInventory.AllCapacity.Value, agentInventory.All.Value);
						return false;
					default:
						Debug.LogError("Unrecognized type: "+model.GetType());
						return false;
				}
			}
			
			bool GetBestInventoryForDelivery(
				IBaseInventoryModel parent,
				out InventoryTransaction transaction
			)
			{
				var minimumOverflowWeight = int.MaxValue;
				Func<InventoryTransaction> getTransaction = null;
				transaction = null;

				foreach (var model in parent.Inventories)
				{
					Func<InventoryTransaction> currentGetTransaction;
					var currentOverflow = Inventory.Empty;
					switch (model)
					{
						case InventoryComponent inventory:
							if (!inventory.Permission.Value.CanDeposit(Agent)) continue;

							inventory.AvailableCapacity.Value.AddClamped(
								inventory.Available.Value,
								SourceState.cache.Transaction.Items,
								out currentOverflow
							);

							currentGetTransaction = () => inventory.RequestDeliver(SourceState.cache.Transaction.Items);
							break;
						default:
							Debug.LogError("Unrecognized type: " + model.GetType());
							continue;
					}

					if (currentOverflow.TotalWeight < minimumOverflowWeight)
					{
						minimumOverflowWeight = currentOverflow.TotalWeight;
						getTransaction = currentGetTransaction;
						if (minimumOverflowWeight == 0) break;
					}
				}

				if (minimumOverflowWeight == int.MaxValue) return false;

				if (getTransaction == null)
				{
					Debug.LogError("No transaction creator was specified");
					return false;
				}

				transaction = getTransaction();
				
				return true;
			}
		}

		class ToNavigateToTarget : AgentTransition<S1, BaseNavigateState<S1, A>, GameModel, A>
		{
			public override string Name => "ToNavigateTo" + transactionType + "Target";
			
			InventoryTransaction.Types transactionType;

			public ToNavigateToTarget(InventoryTransaction.Types transactionType) => this.transactionType = transactionType;
			
			public override bool IsTriggered()
			{
				return SourceState.cache.IsNavigable && transactionType == SourceState.cache.Transaction.Type;
			}

			public override void Transition() => Agent.NavigationPlan.Value = NavigationPlan.Navigating(SourceState.cache.NavigationResult.Path);
		}

		class ToReturnOnTimeout : AgentTransition<S1, S0, GameModel, A>
		{
			public override string Name => "ToReturnOn" + transactionType + "Timeout";
			
			InventoryTransaction.Types transactionType;

			public ToReturnOnTimeout(InventoryTransaction.Types transactionType) => this.transactionType = transactionType;

			public override bool IsTriggered()
			{
				if (SourceState.timeouts <= SourceState.timeoutsLimit) return false;
				return transactionType == SourceState.cache.Transaction.Type;
			}

			public override void Transition()
			{
				SourceState.timeouts = 0;
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