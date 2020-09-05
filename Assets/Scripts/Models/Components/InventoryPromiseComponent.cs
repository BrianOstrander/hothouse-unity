using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.Satchel;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public interface IInventoryPromiseModel : IInventoryModel, IHealthModel
	{
		InventoryPromiseComponent InventoryPromises { get; }
	}
	
	public class InventoryPromiseComponent : ComponentModel<IInventoryPromiseModel>
	{
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

		protected override void OnInitialize()
		{
			Debug.Log("Initializing...");
		}

		public override void Bind()
		{
			Debug.Log("Binding...");
			Model.Health.Destroyed += OnHealthDestroyed;
		}
		
		public override void UnBind()
		{
			Model.Health.Destroyed -= OnHealthDestroyed;
		}
		
		#region HealthComponent Events
		void OnHealthDestroyed(Damage.Result result)
		{
			var destroyed = new List<Stack>();
			
			foreach (var (item, stack) in Model.Inventory.Container.All().ToArray())
			{
				var type = item[Items.Keys.Shared.Type];

				if (type == Items.Values.Shared.Types.Reservation)
				{
					destroyed.Add(stack);
					
					// if (item[Items.Keys.Reservation.])
					//
					
				}
				else if (type == Items.Values.Shared.Types.Transfer)
				{
					destroyed.Add(stack);

					try
					{
						OnBreakPromiseForTransfer(item, stack);
					}
					catch (OperationException e)
					{
						Debug.LogException(e);
					}
				}
			}

			Model.Inventory.Container.Destroy(destroyed.ToArray());
		}
		#endregion

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

				var capacityCurrent = capacityItem[Items.Keys.Capacity.CurrentCount];

				if (isOutput) capacityCurrent += reservationItemStack.Count;
				else capacityCurrent -= reservationItemStack.Count;
				
				var delta = capacityItem[Items.Keys.Capacity.TargetCount] - capacityCurrent;
				
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
						(Items.Keys.Capacity.CurrentCount, capacityCurrent)
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
						(Items.Keys.Capacity.CurrentCount, capacityCurrent)
					);

					unPromisedReservationItem[Items.Keys.Reservation.LogisticState] = Items.Values.Reservation.LogisticStates.Input;
				}
				else
				{
					// We want less
					capacityItem.Set(
						(Items.Keys.Capacity.Desire, Items.Values.Capacity.Desires.Distribute),
						(Items.Keys.Capacity.CurrentCount, capacityCurrent)
					);
					
					unPromisedReservationItem[Items.Keys.Reservation.LogisticState] = Items.Values.Reservation.LogisticStates.Output;
				}

				reservationContainer.Deposit(unPromisedReservationStack);
			}
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