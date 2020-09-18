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
		class CalculateCache
		{
			public Dictionary<long, ItemStack> Resources = new Dictionary<long, ItemStack>();
			public Dictionary<long, ItemStack> CapacityPools = new Dictionary<long, ItemStack>();
			public Dictionary<long, ItemStack> Capacities = new Dictionary<long, ItemStack>();
			public Dictionary<long, ItemStack> Reservations = new Dictionary<long, ItemStack>();
			
			public Dictionary<long, List<long>> CapacityPoolToResources = new Dictionary<long, List<long>>();
			public Dictionary<long, List<long>> CapacityPoolToCapacities = new Dictionary<long, List<long>>();
			public Dictionary<long, List<long>> CapacityToReservations = new Dictionary<long, List<long>>();

			public void Link(
				long key,
				long value,
				Dictionary<long, List<long>> dictionary
			)
			{
				if (!dictionary.TryGetValue(key, out var list))
				{
					list = new List<long>();
					dictionary[key] = list;
				}
				
				list.Add(value);
			}
			
			public void Link(
				long key,
				long value,
				Dictionary<long, long> dictionary
			)
			{
				try { dictionary.Add(key, value); }
				catch (ArgumentException e) { Debug.LogException(e); }
			}
		}
		
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

		public void Calculate(params long[] capacityPools)
		{
			var cache = new CalculateCache();
			
			foreach (var itemStack in Container.All())
			{
				var type = itemStack.Item[Items.Keys.Shared.Type];
			
				if (type == Items.Values.Shared.Types.Resource)
				{
					var parentPool = itemStack.Item[Items.Keys.Resource.ParentPool];
					
					if (parentPool == IdCounter.UndefinedId) Debug.LogError($"Undefined {Items.Keys.Resource.ParentPool} for {itemStack}");
					else
					{
						cache.Resources.Add(
							itemStack.Id,
							itemStack
						);
						
						cache.Link(
							parentPool,
							itemStack.Id,
							cache.CapacityPoolToResources
						);
					}
				}
				else if (type == Items.Values.Shared.Types.Capacity)
				{
					var capacityPoolId = itemStack.Item[Items.Keys.Capacity.Pool];
					var isPool = capacityPoolId == IdCounter.UndefinedId;
					
					if (capacityPools.Any() && !capacityPools.Contains(isPool ? itemStack.Id : capacityPoolId)) continue;
					
					if (isPool)
					{
						cache.CapacityPools.Add(
							itemStack.Id,
							itemStack
						);
					}
					else
					{
						cache.Capacities.Add(
							itemStack.Id,
							itemStack
						);
						
						cache.Link(
							capacityPoolId,
							itemStack.Id,
							cache.CapacityPoolToCapacities
						);
					}
				}
				else if (type == Items.Values.Shared.Types.Reservation)
				{
					cache.Reservations.Add(
						itemStack.Id,
						itemStack
					);

					if (itemStack.Item.TryGet(Items.Keys.Reservation.CapacityId, out var capacityId))
					{
						cache.Link(
							capacityId,
							itemStack.Id,
							cache.CapacityToReservations
						);
					}
					else Debug.LogError($"Cannot find {Items.Keys.Reservation.CapacityId} for reservation {itemStack}");
				}
			}
			
			foreach (var capacityPool in cache.CapacityPools.Values)
			{
				var capacityPoolCountCurrent = 0;

				var capacities = cache.CapacityPoolToCapacities[capacityPool.Id]
					.Select(i => cache.Capacities[i])
					.FilteredSelect(
						(ItemStack i, out (ItemStack ItemStack, PropertyFilter Filter) result) =>
						{
							result = (i, default);
							
							if (!Capacities.TryGetValue(i.Id, out result.Filter))
							{
								Debug.LogError($"No capacity filter for {i}");
								return false;
							}

							return true;
						}
					)
					.ToArray();
				
				var capacityToResourceCount = new Dictionary<long, int>();

				if (cache.CapacityPoolToResources.TryGetValue(capacityPool.Id, out var resources))
				{
					foreach (var resource in resources.Select(i => cache.Resources[i]))
					{
						capacityPoolCountCurrent += resource.Count;

						foreach (var capacity in capacities)
						{
							if (capacity.Filter.Validate(resource.Item))
							{
								capacityToResourceCount.TryGetValue(capacity.ItemStack.Id, out var count);
								capacityToResourceCount[capacity.ItemStack.Id] = count + resource.Count;
							}
						}
					}
				}

				foreach (var capacity in capacities.Select(c => c.ItemStack.Item))
				{
					capacityToResourceCount.TryGetValue(capacity.Id, out var capacityCountCurrent);
					capacity[Items.Keys.Capacity.CountCurrent] = capacityCountCurrent;
					var capacityCountTarget = capacity[Items.Keys.Capacity.CountTarget];

					var capacityCountDelta = capacityCountTarget - capacityCountCurrent;

					var reservationCountInputCount = 0;
					var reservationCountInputs = new Dictionary<long, int>();
					
					var reservationCountOutputCount = 0;
					var reservationCountOutputs = new Dictionary<long, int>();

					if (cache.CapacityToReservations.TryGetValue(capacity.Id, out var reservations))
					{
						foreach (var reservation in reservations.Select(r => cache.Reservations[r]))
						{
							Debug.LogError(reservation);
							var reservationLogisticState = reservation.Item[Items.Keys.Reservation.LogisticState];

							if (reservationLogisticState == Items.Values.Reservation.LogisticStates.Input)
							{
								reservationCountInputCount += reservation.Count;
								reservationCountInputs.Add(reservation.Id, reservation.Count);
							}
							else if (reservationLogisticState == Items.Values.Reservation.LogisticStates.Output)
							{
								reservationCountOutputCount += reservation.Count;
								reservationCountOutputs.Add(reservation.Id, reservation.Count);
							}
							else Debug.LogError($"Unrecognized {Items.Keys.Reservation.LogisticState} {reservationLogisticState} for {reservation}");
						}	
					}

					var delta = capacityCountDelta + (reservationCountOutputCount - reservationCountInputCount);

					if (0 < delta)
					{
						Container.New(
							delta,
							Items.Instantiate.Reservation
								.OfInput(
									capacity.Id,
									capacityPool.Id
								)
						);
					}
					else if (delta < 0)
					{
						Container.New(
							Mathf.Abs(delta),
							Items.Instantiate.Reservation
								.OfOutput(
									capacity.Id,
									capacityPool.Id
								)
						);
					}
					
					Debug.Log($"{Container.Id}.{capacity.Id}: {delta}");
				}

				capacityPool.Item[Items.Keys.Capacity.CountCurrent] = capacityPoolCountCurrent;

				// Debug.Log(Container);
				// Debug.LogError($"{Container.Id} cappoolcurr: {capacityPoolCountCurrent}{capacityToResourceCount.Aggregate(string.Empty, (r, c) => $"{r}\n{c.Key} : {c.Value}")}");
			}
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
			Debug.LogError("todo");
		}

		#region HealthComponent Events
		void OnHealthDestroyed(Damage.Result result)
		{
			var droppedItems = new List<Stack>();

			foreach (var element in Container.All(i => i[Items.Keys.Shared.Type] == Items.Values.Shared.Types.Resource))
			{
				var logisticState = element.Item[Items.Keys.Resource.LogisticState];

				if (logisticState == Items.Values.Resource.LogisticStates.None)
				{
					droppedItems.Add(element.Stack);
					continue;
				}

				if (logisticState == Items.Values.Resource.LogisticStates.Output)
				{
					Debug.LogError("TODO: handle destroyed inventories with Output resources...");
					continue;
				}
				
				Debug.LogError($"Unrecognized {Items.Keys.Resource.LogisticState}: {StringExtensions.GetNonNullOrEmpty(logisticState, "< null >", "< empty >")} on {element.Item}");
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