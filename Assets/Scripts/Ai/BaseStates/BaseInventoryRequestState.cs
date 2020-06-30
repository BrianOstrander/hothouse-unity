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
		
		class ToTimeoutOnTarget : AgentTransition<S1, BaseTimeoutState<S1, A>, GameModel, A>
		{
			public override string Name => "ToTimeoutOn" + transactionType + "Target";
			
			InventoryTransaction.Types transactionType;

			InventoryTransaction nextTransaction;
			
			public ToTimeoutOnTarget(InventoryTransaction.Types transactionType) => this.transactionType = transactionType;
			
			public override bool IsTriggered()
			{
				if (!SourceState.cache.IsNavigable) return false;
				if (transactionType != SourceState.cache.Transaction.Type) return false;
				if (SourceState.cache.NavigationRadiusMaximum < Vector3.Distance(Agent.Transform.Position.Value, SourceState.cache.NavigationResult.Target)) return false;

				nextTransaction = null;
				
				switch (transactionType)
				{
					case InventoryTransaction.Types.Deliver:
						break;
					case InventoryTransaction.Types.Distribute:
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

				bool? completionSuccessful = null;
				var overflow = Inventory.Empty;
				
				switch (SourceState.cache.Target)
				{
					case InventoryComponent inventory:
						switch (SourceState.cache.Transaction.Type)
						{
							case InventoryTransaction.Types.Deliver:
								completionSuccessful = inventory.CompleteDeliver(
									SourceState.cache.Transaction,
									out overflow
								);
								Agent.Inventory.Remove(
									SourceState.cache.Transaction.Items - overflow
								);
								break;
							case InventoryTransaction.Types.Distribute:
								completionSuccessful = inventory.CompleteDistribution(
									SourceState.cache.Transaction,
									out overflow
								);

								if (!nextTransaction.Items.Contains(SourceState.cache.Transaction.Items))
								{
									Debug.LogError("TODO: Handle what happens when we can't distribute everything...");
								}
								
								Agent.Inventory.Add(nextTransaction.Items);
								Agent.InventoryPromises.Transactions.Push(nextTransaction);
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

				if (completionSuccessful.HasValue)
				{
					if (completionSuccessful.Value)
					{
						if (!overflow.IsEmpty) Debug.LogWarning("Unable to " + transactionType + " all resources, this probably is ok, but maybe not?\n" + overflow);
						SourceState.timeoutState.ConfigureForInterval(Interval.WithMaximum(1f)); // TODO: Don't hardcode this...	
					}
					else
					{
						Debug.LogWarning("Not able to " + transactionType + " any resources, this probably is ok, but maybe not?");
						SourceState.timeoutState.ConfigureForInterval(Interval.Zero());
					}
				}
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
					case AgentInventoryComponent agentInventory:
						return hasCapacityFor(agentInventory.AllCapacity.Value, agentInventory.All.Value);
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

							currentGetTransaction = () =>
							{
								var isRequestDeliverValid = inventory.RequestDeliver(
									SourceState.cache.Transaction.Items,
									out var currentTransaction,
									out var requestDeliverOverflow
								);
								
								if (!isRequestDeliverValid) Debug.LogError("Invalid request to deliver made, this is unexpected");
								if (!requestDeliverOverflow.IsEmpty) Debug.LogError("Overflow on request to deliver, this is unexpected");
								
								return currentTransaction;
							};
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