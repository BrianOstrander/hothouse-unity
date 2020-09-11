using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Ai;
using Lunra.Satchel;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public interface IInventoryPromiseModel : IAgentModel
	{
		InventoryPromiseComponent InventoryPromises { get; }
	}
	
	public class InventoryPromiseComponent : ComponentModel<IInventoryPromiseModel>
	{
		public struct TransferInfo
		{
			public Container Container;
			public Item CapacityPool;
			public Item Capacity;
			public Item Reservation;
		}
		
		public struct ProcessResult
		{
			public enum Actions
			{
				Unknown = 0,
				None = 10,
				Navigate = 20,
				Timeout = 30
			}
		
			public static ProcessResult None() => new ProcessResult(Actions.None);

			public Actions Action { get; }
			public NavigationPlan Navigation { get; }

			public ProcessResult(
				Actions action,
				NavigationPlan navigation = default
			)
			{
				Action = action;
				Navigation = navigation;
			}
		}
		
		class OperationException : Exception
		{
			public OperationException(string message) : base(message) {}
		}
		
		#region Serialized
		[JsonProperty] List<long> all = new List<long>();
		[JsonIgnore] public StackProperty<long> All { get; } 
		#endregion
		
		#region Non Serialized
		#endregion

		public InventoryPromiseComponent()
		{
			All = new StackProperty<long>(all);
		}

		public override void Bind()
		{
			Model.Health.Destroyed += OnHealthDestroyed;
		}
		
		public override void UnBind()
		{
			Model.Health.Destroyed -= OnHealthDestroyed;
		}
		
		#region HealthComponent Events
		void OnHealthDestroyed(Damage.Result result) => BreakAll();
		#endregion

		public ProcessResult Process()
		{
			while (All.TryPeek(out var promiseId))
			{
				try
				{
					if (Model.Inventory.Container.TryFindFirst(promiseId, out var promiseItem))
					{
						var type = promiseItem[Items.Keys.Shared.Type];
				
						if (type == Items.Values.Shared.Types.Transfer)
						{
							var state = promiseItem[Items.Keys.Transfer.LogisticState];

							var isValid = false;
							ProcessResult result;
							
							if (state == Items.Values.Transfer.LogisticStates.Pickup)
							{
								isValid = OnProcessTransfer(
									promiseItem,
									true,
									out result
								);
							}
							else if (state == Items.Values.Transfer.LogisticStates.Dropoff)
							{
								isValid = OnProcessTransfer(
									promiseItem,
									false,
									out result
								);
							}
							else throw new OperationException($"Unrecognized {Items.Keys.Transfer.LogisticState}: {state}");

							if (isValid) return result;
						}
						else throw new OperationException($"Unrecognized {Items.Keys.Shared.Type}: {type}");
					}
					else throw new OperationException($"Cannot find item for promise with id {promiseId}");

					// If we get here the promise was not invalid, just impossible to complete for some reason - usually
					// because we couldn't navigate there - so we simply break the promise. Ideally no errors occur.
					Break();
				}
				catch (OperationException e)
				{
					// The promise was invalid in some way, missing pickup or dropoff location, and as such we just pop
					// it, since breaking the promise would cause more errors.
					All.Pop();
					Debug.LogException(e);
				}
			}
			
			return ProcessResult.None();
		}

		bool OnProcessTransfer(
			Item transferItem,
			bool isPickup,
			out ProcessResult result
		)
		{
			var reservationIdKey = isPickup ? Items.Keys.Transfer.ReservationPickupId : Items.Keys.Transfer.ReservationDropoffId;
			var itemId = transferItem[Items.Keys.Transfer.ItemId];

			if (!Game.Items.TryGet(itemId, out var item))
			{
				throw new OperationException($"Unable to find item {itemId} of transfer {transferItem}");
			}

			var reservationItemId = transferItem[reservationIdKey];
			if (!Game.Items.TryGet(reservationItemId, out var reservationItem))
			{
				throw new OperationException($"Unable to find {reservationIdKey} for reservation {reservationItemId} of transfer {transferItem}");
			}

			var reservationContainerId = reservationItem.ContainerId;

			if (!Game.Query.TryFindFirst<IInventoryModel>(m => m.Inventory.Container.Id == reservationContainerId, out var reservationInventory))
			{
				throw new OperationException($"Unable to find an inventory with container id {reservationContainerId} for reservation {reservationItem} of transfer {transferItem}");
			}

			if (!Navigation.TryQuery(reservationInventory, out var navigationQuery))
			{
				throw new OperationException($"Unable to create a navigation query for {reservationInventory}");
			}
			
			var isNavigable = NavigationUtility.CalculateNearest(
				Model.Transform.Position.Value,
				out var navigationResult,
				navigationQuery
			);

			if (!isNavigable)
			{
				result = default;
				return false;
			}

			if (Model.InteractionRadius.Value < Vector3.Distance(Model.Transform.Position.Value, navigationResult.Target))
			{
				result = new ProcessResult(
					ProcessResult.Actions.Navigate,
					NavigationPlan.Navigating(
						navigationResult.Path,
						NavigationPlan.Interrupts.RadiusThreshold,
						Model.InteractionRadius.Value
					)
				);
				return true;
			}

			if (isPickup)
			{
				OnProcessPickup(
					transferItem,
					reservationItem,
					item,
					reservationInventory
				);
			}
			else
			{
				OnProcessDropoff(
					transferItem,
					reservationItem,
					item,
					reservationInventory
				);
			}
			
			result = new ProcessResult(ProcessResult.Actions.Timeout);

			return true;
		}

		void OnProcessPickup(
			Item transferItem,
			Item reservationItem,
			Item item,
			IInventoryModel reservationInventory
		)
		{
			reservationInventory.Inventory.Container
				.Destroy(reservationItem);

			Container.Transfer(
				item.StackOfAll(),
				reservationInventory.Inventory.Container,
				Model.Inventory.Container
			);
			
			var capacityId = reservationItem[Items.Keys.Reservation.CapacityId];
			
			if (reservationInventory.Inventory.Container.TryFindFirst(capacityId, out var capacityDistribute))
			{
				capacityDistribute[Items.Keys.Capacity.CountCurrent]--;
						
				if (capacityDistribute[Items.Keys.Capacity.CountCurrent] == capacityDistribute[Items.Keys.Capacity.CountTarget])
				{
					capacityDistribute[Items.Keys.Capacity.Desire] = Items.Values.Capacity.Desires.None;
				}
			}
			else throw new OperationException($"Unable to find capacity item [ {capacityId} ] referenced by reservation {reservationItem}");
				
			item[Items.Keys.Resource.LogisticState] = Items.Values.Resource.LogisticStates.None;
				
			transferItem.Set(
				(Items.Keys.Transfer.ReservationPickupId, IdCounter.UndefinedId),
				(Items.Keys.Transfer.LogisticState, Items.Values.Transfer.LogisticStates.Dropoff)
			);
		}
		
		void OnProcessDropoff(
			Item transferItem,
			Item reservationItem,
			Item item,
			IInventoryModel reservationInventory
		)
		{
			reservationInventory.Inventory.Container
				.Destroy(reservationItem);

			Container.Transfer(
				item.StackOfAll(),
				Model.Inventory.Container,
				reservationInventory.Inventory.Container
			);
				
			Model.Inventory.Container
				.Destroy(transferItem);

			All.Pop();
		}

		public void Break()
		{
			if (!All.TryPop(out var promise)) return;

			if (!Model.Inventory.Container.TryFindFirst(promise, out var promiseItem, out var promiseStack))
			{
				Debug.LogError($"Unable to find promise with item id {promise} in container {Model.Inventory.Container.Id}");
				return;
			}
			
			var type = promiseItem[Items.Keys.Shared.Type];
			
			if (type == Items.Values.Shared.Types.Transfer)
			{
				try
				{
					OnBreakPromiseForTransfer(promiseItem, promiseStack);
				}
				catch (OperationException e)
				{
					Debug.LogException(e);
				}
			}
			else Debug.LogError($"Unrecognized {Items.Keys.Shared.Type}: {type}");
			
			Model.Inventory.Container.Destroy(promiseStack);
		}
		
		public void BreakAll()
		{
			// TODO: This simply breaks promises, doesn't drop the items or anything... some way to handle what to do
			// with formerly promised items might be nice...
			while (All.Any()) Break();
		}

		void OnBreakPromiseForTransfer(
			Item transferItem,
			Stack transferStack
		)
		{
			foreach (var reservationIdKey in new [] { Items.Keys.Transfer.ReservationDropoffId, Items.Keys.Transfer.ReservationPickupId })
			{
				var reservationId = transferItem[reservationIdKey];
				if (reservationId == IdCounter.UndefinedId) continue;
				
				if (!Game.Items.TryGet(reservationId, out var reservationItem)) throw new OperationException($"Unable to find reservation [ {reservationId} ] referenced by {transferItem}");
				
				if (!reservationItem[Items.Keys.Reservation.IsPromised]) throw new OperationException($"Expected reservation {reservationItem} for transfer {transferItem} to be promised, but it was not");

				var reservationState = reservationItem[Items.Keys.Reservation.LogisticState];

				if (!Game.Items.Containers.TryGetValue(reservationItem.ContainerId, out var reservationContainer)) throw new OperationException($"Unable to find container {reservationItem.ContainerId} for reservation {reservationItem} referenced by {transferItem}");

				var reservationItemStacks = reservationContainer.Withdrawal((reservationId, transferStack.Count));
				
				if (reservationItemStacks.Length != 1) throw new OperationException($"Expected 1 reservation item, but withdrew {reservationItemStacks.Length} instead, referenced by {transferItem}");

				var reservationItemStack = reservationItemStacks.First();
				
				if (reservationItemStack.Id != reservationItem.Id) throw new OperationException($"Expected reservation stack to have id {reservationItem.Id}, but found {reservationItemStack.Id} instead, referenced by {transferItem}");

				bool isOutput;

				if (reservationState == Items.Values.Reservation.LogisticStates.Input) isOutput = false;
				else if (reservationState == Items.Values.Reservation.LogisticStates.Output) isOutput = true;
				else throw new OperationException($"Unrecognized {Items.Keys.Reservation.LogisticState} on reservation {reservationItem} for transfer {transferItem}");

				if (isOutput)
				{
					var outputStacks = reservationContainer.Withdrawal((transferItem[Items.Keys.Transfer.ItemId], transferStack.Count));
					
					if (outputStacks.Length != 1) throw new OperationException($"Expected 1 output item, but withdrew {outputStacks.Length} instead, referenced by {transferItem}");
					
					var outputStack = outputStacks.First();

					if (outputStack.Count != transferStack.Count) throw new OperationException($"Expected output stack to have count of {transferStack.Count}, but instead it was {outputStack.Count}, referenced by {transferItem}");
					
					if (!Game.Items.TryGet(outputStack.Id, out var outputItem)) throw new OperationException($"Unable to find item for stack {outputStack}, referenced by {transferItem}");

					var outputItemType = outputItem[Items.Keys.Shared.Type];

					if (outputItemType == Items.Values.Shared.Types.Resource)
					{
						outputItem[Items.Keys.Resource.LogisticState] = Items.Values.Resource.LogisticStates.None;
					}
					else throw new OperationException($"Unrecognized {Items.Keys.Shared.Type} for output item: {outputItem}");

					if (reservationContainer.TryFindFirst(i => i.CanStack(outputItem), out var existingOutputItem))
					{
						reservationContainer.Increment(existingOutputItem.StackOf(outputStack.Count));
					}
					else
					{
						reservationContainer.Deposit(outputStack);
					}
				}
				
				var capacityId = reservationItem[Items.Keys.Reservation.CapacityId];
				if (!Game.Items.TryGet(capacityId, out var capacityItem)) throw new OperationException($"Unable to find capacity [ {capacityId} ] for reservation {reservationItem}, referenced by {transferItem}");

				var capacityCurrent = capacityItem[Items.Keys.Capacity.CountCurrent];

				if (!isOutput) capacityCurrent -= reservationItemStack.Count;
				
				var delta = capacityItem[Items.Keys.Capacity.CountTarget] - capacityCurrent;
				
				var foundReservation = reservationContainer.TryFindFirst(
					out var unPromisedReservationItem,
					out var unPromisedReservationStack,
					(Items.Keys.Reservation.CapacityId, capacityId),
					(Items.Keys.Reservation.IsPromised, false),
					(Items.Keys.Reservation.TransferId, IdCounter.UndefinedId)
				);

				if (delta == 0)
				{
					// We are satisfied	
					capacityItem.Set(
						(Items.Keys.Capacity.Desire, Items.Values.Capacity.Desires.None),
						(Items.Keys.Capacity.CountCurrent, capacityCurrent)
					);

					if (foundReservation) reservationContainer.Destroy(unPromisedReservationStack);
					return;
				}
				
				if (foundReservation)
				{
					reservationContainer.Withdrawal(unPromisedReservationStack);
					unPromisedReservationStack = unPromisedReservationStack.NewCount(Mathf.Abs(delta));
				}
				else
				{
					unPromisedReservationStack = Game.Items.Builder
						.BeginItem()
						.WithProperties(
							Items.Instantiate.Reservation.OfUnknown(capacityId)
						)
						.Done(Mathf.Abs(delta), out unPromisedReservationItem);
				}
				
				if (0 < delta)
				{
					// We want more
					capacityItem.Set(
						(Items.Keys.Capacity.Desire, Items.Values.Capacity.Desires.Receive),
						(Items.Keys.Capacity.CountCurrent, capacityCurrent)
					);

					unPromisedReservationItem[Items.Keys.Reservation.LogisticState] = Items.Values.Reservation.LogisticStates.Input;
				}
				else
				{
					// We want less
					capacityItem.Set(
						(Items.Keys.Capacity.Desire, Items.Values.Capacity.Desires.Distribute),
						(Items.Keys.Capacity.CountCurrent, capacityCurrent)
					);
					
					unPromisedReservationItem[Items.Keys.Reservation.LogisticState] = Items.Values.Reservation.LogisticStates.Output;
				}

				reservationContainer.Deposit(unPromisedReservationStack);
			}
		}
		
		public bool Transfer(
			Item item,
			TransferInfo source,
			TransferInfo destination
		)
		{
			item = Game.Items
				.First(
					source.Container
						.Withdrawal(
							item.StackOf(1)
						).First()
				);
			
			source.Reservation = Game.Items
				.First(
					source.Container
						.Withdrawal(
							source.Reservation.StackOf(1)
						).First()
				);
			
			item[Items.Keys.Resource.LogisticState] = Items.Values.Resource.LogisticStates.Output;

			source.Reservation[Items.Keys.Reservation.IsPromised] = true;

			source.Container.Deposit(item.StackOf(1));
			source.Container.Deposit(source.Reservation.StackOf(1));

			destination.Capacity[Items.Keys.Capacity.CountCurrent]++;

			var isDestinationCapacityAtTarget = destination.Capacity[Items.Keys.Capacity.CountCurrent] == destination.Capacity[Items.Keys.Capacity.CountTarget];

			if (isDestinationCapacityAtTarget) destination.Capacity[Items.Keys.Capacity.Desire] = Items.Values.Capacity.Desires.None;

			destination.Reservation = Game.Items
				.First(
					destination.Container
					.Withdrawal(
						destination.Reservation.StackOf(1)
					).First()
				);
			
			destination.Reservation[Items.Keys.Reservation.IsPromised] = true;

			destination.Container.Deposit(destination.Reservation.StackOf(1));
			
			Model.Inventory.Container.Deposit(
				Game.Items.Builder
					.BeginItem()
					.WithProperties(
						Items.Instantiate.Transfer.Pickup(
							item.Id,
							source.Reservation.Id,
							destination.Reservation.Id
						)	
					)
					.Done(1, out var transfer)
			);

			source.Reservation[Items.Keys.Reservation.TransferId] = transfer.Id;
			destination.Reservation[Items.Keys.Reservation.TransferId] = transfer.Id;
				
			All.Push(transfer.Id);

			if (source.CapacityPool != null) source.CapacityPool[Items.Keys.CapacityPool.CountCurrent]--;

			var destinationCapacityPoolCountCurrent = ++destination.CapacityPool[Items.Keys.CapacityPool.CountCurrent];
			
			return isDestinationCapacityAtTarget || destination.CapacityPool[Items.Keys.CapacityPool.CountMaximum] <= destinationCapacityPoolCountCurrent;
		}
		
		public void Reset()
		{
			All.Clear();
			ResetId();
		}

		public override string ToString()
		{
			var result = "Inventory Promise Component [ " + ShortId + " ]:\n";
			return result;
		}
	}
}