using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.Satchel;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public interface IInventoryModel : IRoomTransformModel
	{
		InventoryComponent Inventory { get; }
	}
	
	public class InventoryComponent : ComponentModel<IInventoryModel>
	{
		class ReservationCache
		{
			public Item Reservation;
			public int ReservationCount;
			public Item Transfer;
			public bool ReservationIsPromised;
			public IInventoryPromiseModel TransferParent;
		}
	
		#region Serialized
		[JsonProperty] public Dictionary<long, PropertyFilter> Capacities { get; private set; } = new Dictionary<long, PropertyFilter>();
		[JsonProperty] public Container Container { get; private set; }
		#endregion
		
		#region Non Serialized
		#endregion

		public override void Bind()
		{
			if (Model is IHealthModel health) health.Health.Destroyed += OnHealthDestroyed;
		}
		
		public override void UnBind()
		{
			if (Model is IHealthModel health) health.Health.Destroyed -= OnHealthDestroyed;
		}

		protected override void OnInitialize() => Container = Container?.Initialize(Game.Items) ?? Game.Items.Builder.Container();

		protected override void OnReset() => Container.Initialize(Game.Items);

		protected override void OnCleanup()
        {
	        Container.Reset();
	        Capacities.Clear();
	        ResetId();
        }

		public void SetForbidden(
			long id,
			bool isForbidden
		)
		{
			if (id == IdCounter.UndefinedId) throw new ArgumentException("Cannot set the capacity for an undefined Id", nameof(id));

			if (!Container.TryFindFirst(id, out var item))
			{
				Debug.LogError($"Cannot find item [ {id} ] in [ {Container.Id} ] of {ShortId}");
				return;
			}

			var type = item[Items.Keys.Shared.Type];

			if (type != Items.Values.Shared.Types.CapacityPool)
			{
				Debug.LogError($"Unrecognized {Items.Keys.Shared.Type}: {type} on [ {id} ] in [ {Container.Id} ] of {ShortId}");
				return;
			}

			var existingIsForbidden = item[Items.Keys.CapacityPool.IsForbidden];

			if (existingIsForbidden == isForbidden) return;
			
			item[Items.Keys.CapacityPool.IsForbidden] = isForbidden;

			if (isForbidden)
			{
				foreach (var (reservation, reservationStack) in Container.All(i => i[Items.Keys.Reservation.CapacityPoolId] == id).ToArray())
				{
					var transferId = reservation[Items.Keys.Reservation.TransferId];

					if (transferId != IdCounter.UndefinedId)
					{
						if (Game.Items.TryGet(transferId, out var transfer))
						{
							if (Game.Query.TryFindFirst<IInventoryPromiseModel>(m => m.Inventory.Container.Id == transfer.ContainerId, out var transferParent))
							{
								// TODO: Seems a bit heavy handed to break all promises...
								transferParent.InventoryPromises.BreakAll();
							}
							else Debug.LogError($"Cannot find parent of transfer {transfer} for reservation {reservation}");
						}
						else Debug.LogError($"Cannot find transfer [ {transferId} ] for reservation {reservation}");

						continue;
					}

					Container.Destroy(reservationStack);
				}
			}
			else
			{
				foreach (var (capacity, _) in Container.All(i => i[Items.Keys.Capacity.CapacityPoolId] == id).ToArray())
				{
					capacity[Items.Keys.Capacity.Desire] = Items.Values.Capacity.Desires.NotCalculated;
				}
				Calculate();
			}
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="id">The Item Id of either a specific Capacity or a CapacityPool.</param>
		/// <param name="countTarget"></param>
		// TODO: Might want to make a separate function for capacity pools...
		public void SetCapacity(
			long id,
			int countTarget
		)
		{
			if (id == IdCounter.UndefinedId) throw new ArgumentException("Cannot set the capacity for an undefined Id", nameof(id));
			if (countTarget < 0) throw new ArgumentOutOfRangeException(nameof(countTarget), "Cannot have a count less than zero");

			if (!Container.TryFindFirst(id, out var item))
			{
				Debug.LogError($"Cannot find item [ {id} ] in [ {Container.Id} ] of {ShortId}");
				return;
			}

			var type = item[Items.Keys.Shared.Type];

			if (type == Items.Values.Shared.Types.Capacity)
			{
				if (OnSetCapacity(item, countTarget, out var countTargetDelta, out var reservationsExisting))
				{
					if (0 < countTargetDelta)
					{
						OnSetCapacityBudgetIncrease(
							item,
							countTargetDelta,
							reservationsExisting
						);
					}
					else
					{
						OnSetCapacityBudgetDecrease(
							item,
							Mathf.Abs(countTargetDelta),
							reservationsExisting
						);
					}
					
					var countCurrent = item[Items.Keys.Capacity.CountCurrent];
					
					if (countCurrent < countTarget) item[Items.Keys.Capacity.Desire] = Items.Values.Capacity.Desires.Receive;
					else if (countTarget < countCurrent) item[Items.Keys.Capacity.Desire] = Items.Values.Capacity.Desires.Distribute;
					else item[Items.Keys.Capacity.Desire] = Items.Values.Capacity.Desires.None;
				}
			}
			else if (type == Items.Values.Shared.Types.CapacityPool)
			{
				OnSetCapacityPool(item, countTarget);
			}
			else Debug.LogError($"Unrecognized {Items.Keys.Shared.Type}: {type} on [ {id} ] in [ {Container.Id} ] of {ShortId}");
		}

		bool OnSetCapacity(
			Item capacity,
			int countTarget,
			out int countTargetDelta,
			out ReservationCache[] reservationsExisting
		)
		{
			reservationsExisting = new ReservationCache[0];
			countTargetDelta = 0;
			
			if (capacity[Items.Keys.Capacity.Desire] == Items.Values.Capacity.Desires.NotCalculated) return false;
		
			var countTargetPrevious = capacity[Items.Keys.Capacity.CountTarget];

			if (Container.TryFindFirst(capacity[Items.Keys.Capacity.CapacityPoolId], out var capacityPool)) countTarget = Mathf.Min(capacityPool[Items.Keys.CapacityPool.CountTarget], countTarget);
			else Debug.LogError($"Cannot find capacity pool for {capacity}");
			
			if (countTargetPrevious == countTarget) return false;

			countTargetDelta = countTarget - countTargetPrevious;
			
			capacity[Items.Keys.Capacity.CountTarget] = countTarget;

			var reservations = Container
				.All(i => i[Items.Keys.Reservation.CapacityId] == capacity.Id)
				.ToArray();

			if (reservations.None()) return countTargetDelta != 0;
			
			var reservationsExistingList = new List<ReservationCache>();
			
			foreach (var reservation in reservations)
			{
				var cache = new ReservationCache
				{
					Reservation = reservation.Item,
					ReservationCount = reservation.Stack.Count
				};

				var transferId = cache.Reservation[Items.Keys.Reservation.TransferId];

				if (transferId != IdCounter.UndefinedId)
				{
					cache.ReservationIsPromised = true;
					
					if (Game.Items.TryGet(transferId, out cache.Transfer))
					{
						if (!Game.Query.TryFindFirst(m => m.Inventory.Container.Id == cache.Transfer.ContainerId, out cache.TransferParent))
						{
							Debug.LogError($"Cannot find transfer parent with inventory container [ {cache.Transfer.ContainerId} ] for transfer {cache.Transfer}");
						}
					}
					else Debug.LogError($"Cannot find transfer [ {transferId} ] for reservation {cache.Reservation}");
				}
				
				reservationsExistingList.Add(cache);
			}

			reservationsExisting = reservationsExistingList.ToArray();
			return countTargetDelta != 0;
		}
		
		void OnSetCapacityBudgetIncrease(
			Item capacity,
			int countTargetDelta,
			ReservationCache[] reservationsExisting
		)
		{
			var destroyed = new List<Stack>();
			
			ReservationCache existingUnpromisedInput = null;
			foreach (var cache in reservationsExisting.Where(r => !r.ReservationIsPromised))
			{
				if (cache.Reservation[Items.Keys.Reservation.LogisticState] == Items.Values.Reservation.LogisticStates.Input)
				{
					if (existingUnpromisedInput != null) Debug.LogError($"Found unpromised input {cache.Reservation}, but already found {existingUnpromisedInput.Reservation}, this may cause unexpected behaviour");
					existingUnpromisedInput = cache;
				}
				else
				{
					var countRemoved = Mathf.Min(countTargetDelta, cache.ReservationCount);
					countTargetDelta -= countRemoved;
					destroyed.Add(cache.Reservation.StackOf(countRemoved));
				}
			}

			if (destroyed.Any()) Container.Destroy(destroyed.ToArray());

			var itemCapacityPoolId = capacity[Items.Keys.Capacity.CapacityPoolId];
			
			if (!Container.TryFindFirst(itemCapacityPoolId, out var capacityPool))
			{
				Debug.LogError($"$Cannot find capacity pool [ {itemCapacityPoolId} ] for capacity {capacity}");
				return;
			}
			
			if (capacityPool[Items.Keys.CapacityPool.IsForbidden]) return;

			if (countTargetDelta != 0)
			{
				if (0 < countTargetDelta)
				{
					if (existingUnpromisedInput == null)
					{
						Container.New(
							countTargetDelta,
							Items.Instantiate.Reservation.OfInput(
								capacity.Id,
								capacity[Items.Keys.Capacity.CapacityPoolId]
							)
						);
					}
					else
					{
						Container.Increment(existingUnpromisedInput.Reservation.StackOf(countTargetDelta));
					}
				
					capacity[Items.Keys.Capacity.Desire] = Items.Values.Capacity.Desires.Receive;
				}
				else Debug.LogError($"Unexpected countTargetValue: {countTargetDelta}");
			}
		}
		
		void OnSetCapacityBudgetDecrease(
			Item capacity,
			int countTargetDelta,
			ReservationCache[] reservationsActive 
		)
		{
			if (countTargetDelta <= 0) throw new ArgumentOutOfRangeException(nameof(countTargetDelta), "Cannot be less than or equal to zero");
			
			var reservationsSorted = reservationsActive
				.OrderBy(r => !r.ReservationIsPromised)
				.ThenBy(r => r.Reservation[Items.Keys.Reservation.LogisticState] == Items.Values.Reservation.LogisticStates.Input)
				.ThenBy(r => r.Reservation[Items.Keys.Reservation.LogisticState] == Items.Values.Reservation.LogisticStates.Output)
				.ToList();

			var destroyed = new List<Stack>();
			var broken = new Dictionary<long, IInventoryPromiseModel>();
			
			ReservationCache existingUnpromisedOutputReservation = null;
			var capacityCountModifications = 0;
			
			while (0 < countTargetDelta && reservationsSorted.Any())
			{
				var reservation = reservationsSorted[0];
				reservationsSorted.RemoveAt(0);
				
				if (reservation.Reservation[Items.Keys.Reservation.LogisticState] == Items.Values.Reservation.LogisticStates.Output)
				{
					if (reservation.ReservationIsPromised)
					{
						countTargetDelta -= reservation.ReservationCount;
					}
					else
					{
						if (existingUnpromisedOutputReservation != null) Debug.LogError($"Found unpromised output {reservation.Reservation}, but {existingUnpromisedOutputReservation.Reservation} was already found, unexpected behaviour may occur");
						existingUnpromisedOutputReservation = reservation;
					}
					continue;
				}
				
				countTargetDelta -= reservation.ReservationCount;
				
				// Must be input at this point...
				
				if (reservation.ReservationIsPromised)
				{
					broken[reservation.Transfer.ContainerId] = reservation.TransferParent;
					reservation.Transfer[Items.Keys.Transfer.ReservationInputId] = IdCounter.UndefinedId;
					destroyed.Add(reservation.Reservation.StackOf(reservation.ReservationCount));
					capacityCountModifications += reservation.ReservationCount;
				}
				else
				{
					// To avoid undershooting we make sure we don't destroy all inputs if any are desired...
					destroyed.Add(
						reservation.Reservation.StackOf(
							0 <= countTargetDelta ? reservation.ReservationCount : (countTargetDelta + reservation.ReservationCount)
						)
					);
				}
			}

			Container.Destroy(destroyed.ToArray());
			// TODO: Super weird to break all of this...
			foreach (var b in broken.Values) b.InventoryPromises.BreakAll();

			var capacityPoolId = capacity[Items.Keys.Capacity.CapacityPoolId];
			
			if (!Container.TryFindFirst(capacityPoolId, out var capacityPool)) Debug.LogError($"Cannot find capacity pool [ {capacityPoolId} ] for {capacity}");
			
			// When input promises are broken, we need to remember to modify the capacities...
			if (0 < capacityCountModifications)
			{
				capacity[Items.Keys.Capacity.CountCurrent] -= capacityCountModifications;
				capacityPool[Items.Keys.CapacityPool.CountCurrent] -= capacityCountModifications;
			}

			if (0 < countTargetDelta && !capacityPool[Items.Keys.CapacityPool.IsForbidden])
			{
				if (existingUnpromisedOutputReservation == null)
				{
					Container.New(
						countTargetDelta,
						Items.Instantiate.Reservation.OfOutput(
							capacity.Id,
							capacityPoolId
						)
					);
				}
				else
				{
					Container.Increment(
						existingUnpromisedOutputReservation.Reservation.StackOf(countTargetDelta)
					);
				}
			}
		}
		
		void OnSetCapacityPool(
			Item capacityPool,
			int count
		)
		{
			var previousCapacityPoolCountTarget = capacityPool[Items.Keys.CapacityPool.CountTarget];

			if (count == previousCapacityPoolCountTarget) return;

			capacityPool[Items.Keys.CapacityPool.CountTarget] = count;

			foreach (var (capacity, _) in Container.All(i => i[Items.Keys.Capacity.CapacityPoolId] == capacityPool.Id).ToArray())
			{
				var capacityCountTarget = capacity[Items.Keys.Capacity.CountTarget];

				if (count != capacityCountTarget)
				{
					SetCapacity(capacity.Id, Mathf.Min(count, capacity[Items.Keys.Capacity.CountTarget]));
				}
			}
		}
		
		public void Calculate()
		{
			var capacityPools = new Dictionary<long, Item>();

			foreach (var (item, _) in Container.All().ToList())
			{
				var type = item[Items.Keys.Shared.Type];

				if (type == Items.Values.Shared.Types.Capacity) OnCalculateCapacity(item);
				else if (type == Items.Values.Shared.Types.CapacityPool) capacityPools.Add(item.Id, item);
			}
			
			foreach (var capacityPool in capacityPools.Values)
			{
				OnCalculateCapacityPool(capacityPool);
			}
		}

		// TODO: I think I can remove the return on this...
		string OnCalculateCapacity(Item capacity)
		{
			var desire = capacity[Items.Keys.Capacity.Desire];
			if (desire != Items.Values.Capacity.Desires.NotCalculated) return desire;

			if (!Capacities.TryGetValue(capacity.Id, out var filter))
			{
				Debug.LogError($"Cannot find filter [ {capacity.Id} ] for capacity {capacity}");
				return null;
			}

			var capacityCountTarget = capacity[Items.Keys.Capacity.CountTarget];
		
			var capacityPoolId = capacity[Items.Keys.Capacity.CapacityPoolId];
			var resourceTotalCount = 0;

			foreach (var (resource, stack) in Container.All(i => i.TryGet(Items.Keys.Resource.LogisticState, out var logisticState) && logisticState == Items.Values.Resource.LogisticStates.None))
			{
				if (resource[Items.Keys.Resource.CapacityPoolId] != capacityPoolId) continue;
				if (!filter.Validate(resource)) continue;
				
				resourceTotalCount += stack.Count;
			}

			var capacityPoolCountTarget = int.MaxValue;
			
			if (Container.TryFindFirst(capacityPoolId, out var capacityPool)) capacityPoolCountTarget = capacityPool[Items.Keys.CapacityPool.CountTarget];
			else Debug.LogError($"Cannot find capacity pool with id {capacityPoolId} for {capacity}");

			var delta = Mathf.Min(capacityCountTarget, capacityPoolCountTarget) - resourceTotalCount;

			var reservationsAllowed = !capacityPool[Items.Keys.CapacityPool.IsForbidden];
			
			if (delta == 0)
			{
				// We are satisfied
				capacity.Set(
					(Items.Keys.Capacity.Desire, Items.Values.Capacity.Desires.None),
					(Items.Keys.Capacity.CountCurrent, resourceTotalCount)
				);

				return Items.Values.Capacity.Desires.None;
			}

			if (0 < delta)
			{
				// We want more
				capacity.Set(
					(Items.Keys.Capacity.Desire, Items.Values.Capacity.Desires.Receive),
					(Items.Keys.Capacity.CountCurrent, resourceTotalCount)
				);

				if (reservationsAllowed)
				{
					Container.Deposit(
						Game.Items.Builder
							.BeginItem()
							.WithProperties(
								Items.Instantiate.Reservation.OfInput(
									capacity.Id,
									capacityPoolId
								)
							)
							.Done(delta)
					);
				}

				return Items.Values.Capacity.Desires.Receive;
			}
			
			// We want less
			capacity.Set(
				(Items.Keys.Capacity.Desire, Items.Values.Capacity.Desires.Distribute),
				(Items.Keys.Capacity.CountCurrent, resourceTotalCount)
			);

			if (reservationsAllowed)
			{
				Container.Deposit(
					Game.Items.Builder
						.BeginItem()
						.WithProperties(
							Items.Instantiate.Reservation.OfOutput(
								capacity.Id,
								capacityPoolId
							)
						)
						.Done(Mathf.Abs(delta))
				);
			}

			return Items.Values.Capacity.Desires.Distribute;
		}
		
		void OnCalculateCapacityPool(Item capacityPool)
		{
			var countPrevious = capacityPool[Items.Keys.CapacityPool.CountCurrent];

			var countCurrent = 0;

			foreach (var element in Container.All(i => i.TryGet(Items.Keys.Capacity.CapacityPoolId, out var poolId) && poolId == capacityPool.Id))
			{
				if (element.Item.NoInstances) continue;
				
				var capacityCountCurrent = element.Item[Items.Keys.Capacity.CountCurrent];
				
				countCurrent += capacityCountCurrent;
			}

			if (countPrevious != countCurrent) capacityPool[Items.Keys.CapacityPool.CountCurrent] = countCurrent;
		}
		
		public void Destroy(
			params Stack[] resources
		)
		{
			var affectedCapacityPoolIds = new HashSet<long>();
			var resourcesList = new List<(Item Item, int Count)>();
			
			foreach (var resource in resources)
			{
				if (resource.Count == 0) continue;
				if (!Container.TryFindFirst(resource.Id, out var resourceItem))
				{
					Debug.LogError($"Cannot find resource with id [ {resource.Id} ] in {Container}");
					continue;
				}
				
				var resourceLogisticState = resourceItem[Items.Keys.Resource.LogisticState];

				if (resourceLogisticState != Items.Values.Resource.LogisticStates.None)
				{
					Debug.LogError($"Expected item {resourceItem} to have {Items.Keys.Resource.LogisticState.Key} of {Items.Values.Resource.LogisticStates.None}, but instead it was {resourceLogisticState}");
					continue;
				}

				affectedCapacityPoolIds.Add(resourceItem[Items.Keys.Resource.CapacityPoolId]);
				resourcesList.Add((resourceItem, resource.Count));
			}
			
			// How many are removed from each Capacities current...
			var capacityDeltas = new Dictionary<long, int>();
			var affectedCapacityIdsToReservations = new Dictionary<long, Item>();

			foreach (var (item, _) in Container.All())
			{
				if (affectedCapacityPoolIds.Contains(item.Id))
				{
					if (item[Items.Keys.CapacityPool.IsForbidden]) Debug.LogError($"Destroying items in forbidden capacity pool [ {item.Id} ], unexpected behaviour may occur");
					
					var capacityPoolCountCurrentDelta = 0;

					foreach (var (resource, resourceCount) in resourcesList)
					{
						if (resource[Items.Keys.Resource.CapacityPoolId] == item.Id)
						{
							capacityPoolCountCurrentDelta += resourceCount;
						}
					}

					if (capacityPoolCountCurrentDelta != 0) item[Items.Keys.CapacityPool.CountCurrent] -= capacityPoolCountCurrentDelta;
				}
				else
				{
					var type = item[Items.Keys.Shared.Type];
					
					if (type == Items.Values.Shared.Types.Capacity)
					{
						var capacityPoolId = item[Items.Keys.Capacity.CapacityPoolId];
						foreach (var (resource, resourceCount) in resourcesList)
						{
							if (resource[Items.Keys.Resource.CapacityPoolId] == capacityPoolId)
							{
								if (Capacities.TryGetValue(item.Id, out var filter))
								{
									if (filter.Validate(resource))
									{
										capacityDeltas.TryGetValue(item.Id, out var capacityDelta);
										capacityDelta += resourceCount;
										capacityDeltas[item.Id] = capacityDelta;

										item[Items.Keys.Capacity.CountCurrent] -= resourceCount;
									}
								}
								else Debug.LogError($"Cannot find capacity filter {item.Id}");
							}
						}
					}
					else if (type == Items.Values.Shared.Types.Reservation)
					{
						if (affectedCapacityPoolIds.Contains(item[Items.Keys.Reservation.CapacityPoolId]))
						{
							if (item[Items.Keys.Reservation.TransferId] == IdCounter.UndefinedId)
							{
								try { affectedCapacityIdsToReservations.Add(item[Items.Keys.Reservation.CapacityId], item); }
								catch (ArgumentException e) { Debug.LogException(e); }
							}
						}
					}
				}
			}

			var destroyed = resourcesList
				.Select(e => e.Item.StackOf(e.Count))
				.ToList();
			
			foreach (var capacityDelta in capacityDeltas)
			{
				if (affectedCapacityIdsToReservations.TryGetValue(capacityDelta.Key, out var reservation))
				{
					string desire = null;
					
					if (reservation.InstanceCount == capacityDelta.Value)
					{
						// We wanted to get rid of all these anyways, so no reservations are left.
						destroyed.Add(reservation.StackOfAll());
						desire = Items.Values.Capacity.Desires.None;
					}
					else if (reservation.InstanceCount < capacityDelta.Value)
					{
						// We only wanted to get rid of some of these, so we need to add a few new input reservations.
						var reservationInputCount = capacityDelta.Value - reservation.InstanceCount;

						Container.New(
							reservationInputCount,
							Items.Instantiate.Reservation.OfInput(
								capacityDelta.Key,
								reservation[Items.Keys.Reservation.CapacityPoolId]
							)
						);
						
						destroyed.Add(reservation.StackOfAll());
						
						desire = Items.Values.Capacity.Desires.Receive;
					}
					else
					{
						// We got rid of some we wanted to get rid of anyways, but there are still some we want to get rid of.
						destroyed.Add(reservation.StackOf(capacityDelta.Value));
					}

					if (desire != null)
					{
						if (Container.TryFindFirst(capacityDelta.Key, out var capacityPool))
						{
							capacityPool[Items.Keys.Capacity.Desire] = desire;
						}
						else Debug.LogError($"Cannot find capacity pool [ {capacityDelta.Key} ] in {Container}");
					}
				}
			}

			Container.Destroy(
				destroyed.ToArray()
			);
		}

		#region HealthComponent Events
		void OnHealthDestroyed(Damage.Result result)
		{
			var droppedItems = new List<Stack>();

			foreach (var (item, stack) in Container.All(i => i[Items.Keys.Shared.Type] == Items.Values.Shared.Types.Resource))
			{
				var logisticState = item[Items.Keys.Resource.LogisticState];

				if (logisticState == Items.Values.Resource.LogisticStates.None)
				{
					droppedItems.Add(stack);
					continue;
				}

				if (logisticState == Items.Values.Resource.LogisticStates.Output)
				{
					Debug.LogError("TODO: handle destroyed inventories with Output resources...");
					continue;
				}
				
				Debug.LogError($"Unrecognized {Items.Keys.Resource.LogisticState}: {StringExtensions.GetNonNullOrEmpty(logisticState, "< null >", "< empty >")} on {item}");
			}

			if (droppedItems.Any())
			{
				Game.ItemDrops.Activate(
					Model,
					Quaternion.identity,
					Container.Withdrawal(droppedItems.ToArray())
				);
			}
		}
		#endregion

		public override string ToString()
		{
			var result = "Inventory Component [ " + ShortId + " ]:\n";
			result += Container.ToString(Container.Formats.IncludeItems | Container.Formats.IncludeItemProperties);
			return result;
		}
	}
}