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
		/// <param name="count"></param>
		public void SetCapacity(
			long id,
			int count
		)
		{
			if (id == IdCounter.UndefinedId) throw new ArgumentException("Cannot set the capacity for an undefined Id", nameof(id));
			if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "Cannot have a count less than zero");

			if (!Container.TryFindFirst(id, out var item))
			{
				Debug.LogError($"Cannot find item [ {id} ] in [ {Container.Id} ] of {ShortId}");
				return;
			}

			var type = item[Items.Keys.Shared.Type];
			
			if (type == Items.Values.Shared.Types.Capacity) OnSetCapacity(item, count);
			else if (type == Items.Values.Shared.Types.CapacityPool) OnSetCapacityPool(item, count);
			else Debug.LogError($"Unrecognized {Items.Keys.Shared.Type}: {type} on [ {id} ] in [ {Container.Id} ] of {ShortId}");
		}

		void OnSetCapacity(
			Item item,
			int count
		)
		{
			Debug.LogError("uhh not done yet");
		}
		
		void OnSetCapacityPool(
			Item item,
			int count
		)
		{
			Debug.LogError("uhh not done yet");
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