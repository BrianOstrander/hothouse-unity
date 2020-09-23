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
							var state = promiseItem[Items.Keys.Transfer.ReservationTarget];

							var isValid = false;
							ProcessResult result;
							
							if (state == Items.Values.Transfer.ReservationTargets.Output)
							{
								isValid = OnProcessTransfer(
									promiseItem,
									true,
									out result
								);
							}
							else if (state == Items.Values.Transfer.ReservationTargets.Input)
							{
								isValid = OnProcessTransfer(
									promiseItem,
									false,
									out result
								);
							}
							else throw new OperationException($"Unrecognized {Items.Keys.Transfer.ReservationTarget}: {state}");

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
			bool isOutput,
			out ProcessResult result
		)
		{
			var reservationIdKey = isOutput ? Items.Keys.Transfer.ReservationOutputId : Items.Keys.Transfer.ReservationInputId;
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

			if (isOutput)
			{
				OnProcessOutput(
					transferItem,
					reservationItem,
					item,
					reservationInventory
				);
			}
			else
			{
				OnProcessInput(
					transferItem,
					reservationItem,
					item,
					reservationInventory
				);
			}
			
			result = new ProcessResult(ProcessResult.Actions.Timeout);

			return true;
		}

		/// <summary>
		/// Called when we are taking an item from the output inventory and putting it into this agent's inventory.
		/// </summary>
		/// <param name="transferItem"></param>
		/// <param name="reservationOutputItem"></param>
		/// <param name="item"></param>
		/// <param name="reservationInventory"></param>
		/// <exception cref="OperationException"></exception>
		void OnProcessOutput(
			Item transferItem,
			Item reservationOutputItem,
			Item item,
			IInventoryModel reservationInventory
		)
		{
			reservationInventory.Inventory.Container
				.Destroy(reservationOutputItem);

			Container.Transfer(
				item.StackOfAll(),
				reservationInventory.Inventory.Container,
				Model.Inventory.Container
			);
			
			var capacityId = reservationOutputItem[Items.Keys.Reservation.CapacityId];
			
			if (!reservationInventory.Inventory.Container.TryFindFirst(capacityId, out var capacityOutput)) throw new OperationException($"Unable to find capacity item [ {capacityId} ] referenced by reservation {reservationOutputItem}");
			
			capacityOutput[Items.Keys.Capacity.CountCurrent]--;
						
			if (capacityOutput[Items.Keys.Capacity.CountCurrent] == capacityOutput[Items.Keys.Capacity.CountTarget])
			{
				capacityOutput[Items.Keys.Capacity.Desire] = Items.Values.Capacity.Desires.None;
			}

			item.Set(
				(Items.Keys.Resource.LogisticState, Items.Values.Resource.LogisticStates.Transfer),
				(Items.Keys.Resource.CapacityPoolId, IdCounter.UndefinedId)
			);
				
			transferItem.Set(
				(Items.Keys.Transfer.ReservationOutputId, IdCounter.UndefinedId),
				(Items.Keys.Transfer.ReservationTarget, Items.Values.Transfer.ReservationTargets.Input)
			);
		}
		
		/// <summary>
		/// Called when we are taking an item from this dweller's inventory and putting it into the input inventory.
		/// </summary>
		/// <param name="transferItem"></param>
		/// <param name="reservationInputItem"></param>
		/// <param name="item"></param>
		/// <param name="reservationInventory"></param>
		void OnProcessInput(
			Item transferItem,
			Item reservationInputItem,
			Item item,
			IInventoryModel reservationInventory
		)
		{
			item.Set(
				(Items.Keys.Resource.CapacityPoolId, reservationInputItem[Items.Keys.Reservation.CapacityPoolId]),
				(Items.Keys.Resource.LogisticState, Items.Values.Resource.LogisticStates.None)
			);
			
			reservationInventory.Inventory.Container
				.Destroy(reservationInputItem);

			Container.Transfer(
				item.StackOfAll(),
				Model.Inventory.Container,
				reservationInventory.Inventory.Container
			);
				
			Model.Inventory.Container
				.Destroy(transferItem);

			All.Pop();
		}

		public bool Break() => Break(out _);
		
		public bool Break(out long promiseId)
		{
			if (!All.TryPop(out promiseId)) return false;

			if (!Model.Inventory.Container.TryFindFirst(promiseId, out var promiseItem, out var promiseStack))
			{
				Debug.LogError($"Unable to find promise with item id {promiseId} in container {Model.Inventory.Container.Id}");
				return false;
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

			return true;
		}
		
		public bool BreakAll()
		{
			var anyBroken = false;
			
			// TODO: This simply breaks promises, doesn't drop the items or anything... some way to handle what to do
			// with formerly promised items might be nice...
			while (All.Any())
			{
				anyBroken |= Break();
			}

			return anyBroken;
		}

		void OnBreakPromiseForTransfer(
			Item transferItem,
			Stack transferStack
		)
		{
			void withdrawalItem(
				Container container,
				Action<Stack> done
			)
			{
				var itemId = transferItem[Items.Keys.Transfer.ItemId];

				if (itemId == IdCounter.UndefinedId) return;

				transferItem[Items.Keys.Transfer.ItemId] = IdCounter.UndefinedId;
				
				var itemStacks = container.Withdrawal((itemId, transferStack.Count));

				if (itemStacks.Length != 1) throw new OperationException($"Expected 1 output item, but withdrew {itemStacks.Length} instead, referenced by {transferItem}");

				var itemStack = itemStacks.First();

				if (itemStack.Count != transferStack.Count) throw new OperationException($"Expected output stack to have count of {transferStack.Count}, but instead it was {itemStack.Count}, referenced by {transferItem}");

				if (!Game.Items.TryGet(itemStack.Id, out var item)) throw new OperationException($"Unable to find item for stack {itemStack}, referenced by {transferItem}");

				var itemType = item[Items.Keys.Shared.Type];

				if (itemType == Items.Values.Shared.Types.Resource)
				{
					item[Items.Keys.Resource.LogisticState] = Items.Values.Resource.LogisticStates.None;
				}
				else throw new OperationException($"Unrecognized {Items.Keys.Shared.Type} for output item: {item}");

				done(itemStack);
			}
		
			foreach (var reservationIdKey in new [] { Items.Keys.Transfer.ReservationOutputId, Items.Keys.Transfer.ReservationInputId })
			{
				var reservationId = transferItem[reservationIdKey];
				// If the reservationId is null, that means someone else destroyed the associated reservation.
				if (reservationId == IdCounter.UndefinedId) continue;
				
				if (!Game.Items.TryGet(reservationId, out var reservationItem)) throw new OperationException($"Unable to find reservation [ {reservationId} ] referenced by {transferItem}");
				
				if (reservationItem[Items.Keys.Reservation.TransferId] == IdCounter.UndefinedId) throw new OperationException($"Expected reservation {reservationItem} for transfer {transferItem} to have a defined {Items.Keys.Reservation.TransferId}, but it was not"); 

				var reservationState = reservationItem[Items.Keys.Reservation.LogisticState];

				if (!Game.Items.Containers.TryGetValue(reservationItem.ContainerId, out var reservationContainer)) throw new OperationException($"Unable to find container {reservationItem.ContainerId} for reservation {reservationItem} referenced by {transferItem}");

				var reservationItemStacks = reservationContainer.Withdrawal((reservationId, transferStack.Count));
				
				if (reservationItemStacks.Length != 1) throw new OperationException($"Expected 1 reservation item, but withdrew {reservationItemStacks.Length} instead, referenced by {transferItem}");

				var reservationItemStack = reservationItemStacks.First();
				
				if (reservationItemStack.Id != reservationItem.Id) throw new OperationException($"Expected reservation stack to have id {reservationItem.Id}, but found {reservationItemStack.Id} instead, referenced by {transferItem}");

				bool isReservationStateOutput;

				if (reservationState == Items.Values.Reservation.LogisticStates.Input) isReservationStateOutput = false;
				else if (reservationState == Items.Values.Reservation.LogisticStates.Output) isReservationStateOutput = true;
				else throw new OperationException($"Unrecognized {Items.Keys.Reservation.LogisticState} on reservation {reservationItem} for transfer {transferItem}");

				if (isReservationStateOutput)
				{
					withdrawalItem(
						reservationContainer,
						itemStack => reservationContainer.Combine(itemStack)
					);
				}

				var capacityId = reservationItem[Items.Keys.Reservation.CapacityId];
				if (!Game.Items.TryGet(capacityId, out var capacityItem)) throw new OperationException($"Unable to find capacity [ {capacityId} ] for reservation {reservationItem}, referenced by {transferItem}");
				
				var capacityPoolId = capacityItem[Items.Keys.Capacity.CapacityPoolId];
				
				if (!Game.Items.TryGet(capacityPoolId, out var capacityPoolItem)) throw new OperationException($"Unable to find capacity pool [ {capacityPoolId} ] for reservation {reservationItem}, referenced by {transferItem}");

				var capacityCurrent = capacityItem[Items.Keys.Capacity.CountCurrent];
				
				if (!isReservationStateOutput)
				{
					capacityCurrent -= reservationItemStack.Count;

					capacityPoolItem[Items.Keys.CapacityPool.CountCurrent] -= reservationItemStack.Count;
				}

				// Withdrawn items get cleaned up eventually, so no need to explicitly destroy them...
				if (capacityPoolItem[Items.Keys.CapacityPool.IsForbidden]) continue;
				
				var delta = capacityItem[Items.Keys.Capacity.CountTarget] - capacityCurrent;
				
				var foundReservation = reservationContainer.TryFindFirst(
					out var unPromisedReservationItem,
					out var unPromisedReservationStack,
					(Items.Keys.Reservation.CapacityId, capacityId),
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
					continue;
				}
				
				if (foundReservation)
				{
					reservationContainer.Withdrawal(unPromisedReservationStack);
					unPromisedReservationStack = unPromisedReservationStack.NewCount(Mathf.Abs(delta));
				}
				else
				{
					// I don't really understand what happens here...
					unPromisedReservationStack = Game.Items.Builder
						.BeginItem()
						.WithProperties(
							Items.Instantiate.Reservation.OfUnknown(capacityId, capacityPoolId)
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

			// This will only go off if items hasn't already been handled...
			withdrawalItem(
				Model.Inventory.Container,
				itemStack => Game.ItemDrops.Activate(
					Model,
					Quaternion.identity,
					itemStack
				)
			);
		}
		
		public bool Transfer(
			Item item,
			TransferInfo output,
			TransferInfo input
		)
		{
			item = Game.Items
				.First(
					output.Container
						.Withdrawal(
							item.StackOf(1)
						).First()
				);
			
			output.Reservation = Game.Items
				.First(
					output.Container
						.Withdrawal(
							output.Reservation.StackOf(1)
						).First()
				);
			
			item[Items.Keys.Resource.LogisticState] = Items.Values.Resource.LogisticStates.Output;

			output.Container.Deposit(item.StackOf(1));
			output.Container.Deposit(output.Reservation.StackOf(1));

			input.Capacity[Items.Keys.Capacity.CountCurrent]++;

			var isInputCapacityAtTarget = input.Capacity[Items.Keys.Capacity.CountCurrent] == input.Capacity[Items.Keys.Capacity.CountTarget];

			if (isInputCapacityAtTarget) input.Capacity[Items.Keys.Capacity.Desire] = Items.Values.Capacity.Desires.None;

			input.Reservation = Game.Items
				.First(
					input.Container
					.Withdrawal(
						input.Reservation.StackOf(1)
					).First()
				);

			input.Container.Deposit(input.Reservation.StackOf(1));
			
			Model.Inventory.Container.Deposit(
				Game.Items.Builder
					.BeginItem()
					.WithProperties(
						Items.Instantiate.Transfer.Pickup(
							item.Id,
							output.Reservation.Id,
							input.Reservation.Id
						)	
					)
					.Done(1, out var transfer)
			);

			output.Reservation[Items.Keys.Reservation.TransferId] = transfer.Id;
			input.Reservation[Items.Keys.Reservation.TransferId] = transfer.Id;
				
			All.Push(transfer.Id);

			if (output.CapacityPool != null) output.CapacityPool[Items.Keys.CapacityPool.CountCurrent]--;

			var inputCapacityPoolCountCurrent = ++input.CapacityPool[Items.Keys.CapacityPool.CountCurrent];
			
			return isInputCapacityAtTarget || input.CapacityPool[Items.Keys.CapacityPool.CountTarget] <= inputCapacityPoolCountCurrent;
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