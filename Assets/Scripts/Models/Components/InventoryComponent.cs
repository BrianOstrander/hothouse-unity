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
			public enum LogisticStates
			{
				Unknown = 0,
				Output = 20,
				Input = 30
			}
			
			public Item Reservation;
			public int ReservationCount;
			public Item Transfer;
			public LogisticStates ReservationState;
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

			var reservations = Container
				.All(
					reservation =>
					{
						// If this isn't a reservation, it should return false anyways...
						if (!reservation[Items.Keys.Reservation.IsPromised]) return false;

						var capacityId = reservation[Items.Keys.Reservation.CapacityId];

						// I believe all capacities currently require a capacity pool...
						if (!Container.TryFindFirst(capacityId, out var capacity))
						{
							Debug.LogError($"Cannot find capacity [ {capacityId} ] in [ {Container.Id} ] of {ShortId}");
							return false;
						}

						return capacity[Items.Keys.Capacity.Pool] == id;
					}
				)
				.ToArray();

			var transferParentsHandled = new HashSet<long>();
			
			foreach (var reservation in reservations)
			{
				var transferId = reservation.Item[Items.Keys.Reservation.TransferId];
				if (!Game.Items.TryGet(transferId, out var transfer))
				{
					Debug.LogError($"Cannot find transfer [ {transferId} ] for reservation {reservation.Item}");
					continue;
				}

				if (transfer[Items.Keys.Transfer.LogisticState] == Items.Values.Transfer.LogisticStates.Dropoff)
				{
					Debug.LogError("TODO: Handle what happens to these leaked resources here!");
				}
				
				if (!transferParentsHandled.Add(transfer.ContainerId)) continue;

				var transferParent = Game.Query.FirstOrDefault<IInventoryPromiseModel>(m => m.Inventory.Container.Id == transfer.ContainerId);
				if (transferParent == null)
				{
					Debug.LogError($"Cannot find parent model containing transfer {transfer} for reservation {reservation.Item}");
					continue;
				}
				
				transferParent.InventoryPromises.BreakAll();
			}
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="id">The Item Id of either a specific Capacity or a CapacityPool.</param>
		/// <param name="countTarget"></param>
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
				if (OnSetCapacity(item, countTarget, out var countTargetDelta, out var reservationsActive))
				{
					if (0 < countTargetDelta)
					{
						OnSetCapacityBudgetIncrease(
							item,
							countTargetDelta,
							reservationsActive
						);
					}
					else
					{
						countTargetDelta = Mathf.Abs(countTargetDelta);
						var remainingDecreasesRequired = OnSetCapacityBudgetDecrease(
							item,
							countTargetDelta,
							reservationsActive,
							out _
						);
						
						if (remainingDecreasesRequired) Debug.LogError("Unexpected, there shouldn't be any remaining count targets to remove...");
					}
				}
			}
			else if (type == Items.Values.Shared.Types.CapacityPool) OnSetCapacityPool(item, countTarget);
			else Debug.LogError($"Unrecognized {Items.Keys.Shared.Type}: {type} on [ {id} ] in [ {Container.Id} ] of {ShortId}");
		}

		bool OnSetCapacity(
			Item capacity,
			int countTarget,
			out int countTargetDelta,
			out ReservationCache[] reservationsActive
		)
		{
			reservationsActive = new ReservationCache[0];
			countTargetDelta = 0;
			
			if (capacity[Items.Keys.Capacity.Desire] == Items.Values.Capacity.Desires.NotCalculated) return false;
		
			var countTargetPrevious = capacity[Items.Keys.Capacity.CountTarget];

			if (countTargetPrevious == countTarget) return false;

			countTargetDelta = countTarget - countTargetPrevious;
			
			capacity[Items.Keys.Capacity.CountTarget] = countTarget;

			var reservations = Container
				.All(i => i[Items.Keys.Reservation.CapacityId] == capacity.Id)
				.ToArray();

			if (reservations.None()) return countTargetDelta != 0;
			
			var reservationsActiveList = new List<ReservationCache>();
			
			foreach (var reservation in reservations)
			{
				var cache = new ReservationCache
				{
					Reservation = reservation.Item,
					ReservationCount = reservation.Stack.Count
				};

				var reservationState = cache.Reservation[Items.Keys.Reservation.LogisticState];

				if (reservationState == Items.Values.Reservation.LogisticStates.Input)
				{
					cache.ReservationState = ReservationCache.LogisticStates.Input;
				}
				else if (reservationState == Items.Values.Reservation.LogisticStates.Output)
				{
					cache.ReservationState = ReservationCache.LogisticStates.Output;
				}
				else Debug.LogError($"Unrecognized {Items.Keys.Reservation.LogisticState} {reservationState} on {reservation}");
				
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
				
				if (cache.ReservationState != ReservationCache.LogisticStates.Unknown) reservationsActiveList.Add(cache);
			}

			reservationsActive = reservationsActiveList.ToArray();

			return countTargetDelta != 0;
		}
		
		void OnSetCapacityPool(
			Item capacityPool,
			int count
		)
		{
			Debug.LogError("uhh not done yet");
		}

		void OnSetCapacityBudgetIncrease(
			Item capacity,
			int countTargetDelta,
			ReservationCache[] reservationsActive
		)
		{
			if (countTargetDelta <= 0) throw new ArgumentOutOfRangeException(nameof(countTargetDelta), "Must be greater than zero");

			var found = false;
			foreach (var cache in reservationsActive.Where(r => !r.ReservationIsPromised && r.ReservationState == ReservationCache.LogisticStates.Input))
			{
				Container.Increment(cache.Reservation.StackOf(countTargetDelta));
				found = true;
			}

			if (!found)
			{
				Container.New(
					countTargetDelta,
					Items.Instantiate.Reservation.OfInput(
						capacity.Id
					)
				);
			}

			capacity[Items.Keys.Capacity.Desire] = Items.Values.Capacity.Desires.Receive;
		}
		
		bool OnSetCapacityBudgetDecrease(
			Item capacity,
			int countTargetDelta,
			ReservationCache[] reservationsActive,
			out int countTargetDeltaRemaining,
			bool? isPromisedLimit = null,
			ReservationCache.LogisticStates stateLimit = ReservationCache.LogisticStates.Unknown 
		)
		{
			if (countTargetDelta <= 0) throw new ArgumentOutOfRangeException(nameof(countTargetDelta), "Must be greater than zero");

			var reservationsSorted = reservationsActive
				.OrderBy(r => !r.ReservationIsPromised)
				.ThenBy(r => r.ReservationState == ReservationCache.LogisticStates.Output)
				.ThenBy(r => r.ReservationState == ReservationCache.LogisticStates.Input);

			var destroyed = new List<(ReservationCache Cache, int Count)>();
			var broken = new Dictionary<long, IInventoryPromiseModel>();
			var totalDecrease = 0;
			
			foreach (var cache in reservationsSorted)
			{
				if (isPromisedLimit.HasValue && isPromisedLimit.Value == cache.ReservationIsPromised) break;
				if (cache.ReservationState == stateLimit) break;
				
				var availableForDecrease = Mathf.Min(cache.ReservationCount, countTargetDelta);
				destroyed.Add((cache, availableForDecrease));

				if (cache.ReservationIsPromised)
				{
					totalDecrease += availableForDecrease;
					
					switch (cache.ReservationState)
					{
						case ReservationCache.LogisticStates.Output:
							broken[cache.Transfer.ContainerId] = cache.TransferParent;
							cache.Transfer[Items.Keys.Transfer.ReservationPickupId] = IdCounter.UndefinedId;
							break;
						case ReservationCache.LogisticStates.Input:
							broken[cache.Transfer.ContainerId] = cache.TransferParent;
							cache.Transfer[Items.Keys.Transfer.ReservationDropoffId] = IdCounter.UndefinedId;
							break;
						default:
							Debug.LogError($"Unrecognized LogisticState {cache.ReservationState} on reservation {cache.Reservation}");
							break;
					}
				}

				// When destroyed we mark them as unknown so we don't try to destroy them twice...
				cache.ReservationState = ReservationCache.LogisticStates.Unknown;

				countTargetDelta -= availableForDecrease;
				
				if (countTargetDelta == 0) break;
			}

			countTargetDeltaRemaining = countTargetDelta;

			if (destroyed.None()) return false;

			capacity[Items.Keys.Capacity.CountCurrent] -= totalDecrease;

			var countCurrent = capacity[Items.Keys.Capacity.CountCurrent];
			var countTarget = capacity[Items.Keys.Capacity.CountTarget];

			if (countCurrent < countTarget) capacity[Items.Keys.Capacity.Desire] = Items.Values.Capacity.Desires.Receive;
			else if (countTarget < countCurrent) capacity[Items.Keys.Capacity.Desire] = Items.Values.Capacity.Desires.Distribute;
			else capacity[Items.Keys.Capacity.Desire] = Items.Values.Capacity.Desires.None;
			
			Container.Destroy(
				destroyed
					.Select(
						d =>
						{
							d.Cache.ReservationState = ReservationCache.LogisticStates.Unknown;
							return d.Cache.Reservation.StackOf(d.Count);
						}
					).ToArray()
			);

			foreach (var b in broken.Values) b.InventoryPromises.BreakAll();

			return countTargetDeltaRemaining != 0;
		}
		
		// void OnSetCapacityBudgetDecrease(
		// 	int countTargetDelta,
		// 	ReservationCache[] reservationsActive
		// )
		// {
		// 	countTargetDelta = Mathf.Abs(countTargetDelta);
		//
		// 	var reservationsSorted = reservationsActive
		// 		.OrderBy(r => r.TransferState == ReservationCache.LogisticStates.NotDefined)
		// 		.ThenBy(r => r.TransferState == ReservationCache.LogisticStates.Pickup)
		// 		.ThenBy(r => r.TransferState == ReservationCache.LogisticStates.Dropoff);
		//
		// 	foreach (var cache in reservationsSorted)
		// 	{
		// 		
		//
		// 		if (--countTargetDelta == 0) break;
		// 	}
		// }
		
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