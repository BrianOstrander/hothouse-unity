using System;
using System.Linq;
using Lunra.StyxMvp.Models;
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
		[JsonProperty] InventoryPermission permission = InventoryPermission.AllForAnyJob();
		[JsonProperty] public ListenerProperty<InventoryPermission> Permission { get; private set; }

		[JsonProperty] Inventory available = Inventory.Empty;
		readonly ListenerProperty<Inventory> availableListener;
		[JsonIgnore] public ReadonlyProperty<Inventory> Available { get; }
		[JsonProperty] Inventory availableWithoutDesire = Inventory.Empty;
		readonly ListenerProperty<Inventory> availableWithoutDesireListener;
		[JsonIgnore] public ReadonlyProperty<Inventory> AvailableWithoutDesire { get; }
		
		[JsonProperty] Inventory forbidden = Inventory.Empty;
		readonly ListenerProperty<Inventory> forbiddenListener;
		[JsonIgnore] public ReadonlyProperty<Inventory> Forbidden { get; }

		[JsonProperty] InventoryCapacity reservedCapacity = InventoryCapacity.None();
		readonly ListenerProperty<InventoryCapacity> reservedCapacityListener;
		[JsonIgnore] public ReadonlyProperty<InventoryCapacity> ReservedCapacity { get; }

		[JsonProperty] InventoryCapacity availableCapacity = InventoryCapacity.None();
		readonly ListenerProperty<InventoryCapacity> availableCapacityListener;
		[JsonIgnore] public ReadonlyProperty<InventoryCapacity> AvailableCapacity { get; }

		[JsonProperty] InventoryDesire desired = InventoryDesire.Ignored();
		[JsonIgnore] public ListenerProperty<InventoryDesire> Desired { get; }
		
		[JsonProperty] Inventory all = Inventory.Empty;
		protected readonly ListenerProperty<Inventory> AllListener;
		[JsonIgnore] public ReadonlyProperty<Inventory> All { get; }
		
		[JsonProperty] InventoryCapacity allCapacity = InventoryCapacity.None();
		protected readonly ListenerProperty<InventoryCapacity> AllCapacityListener;
		[JsonIgnore] public ReadonlyProperty<InventoryCapacity> AllCapacity { get; }
		#endregion
		
		#region Non Serialized
		#endregion

		public bool IsFull() => AllCapacity.Value.IsFull(All.Value);
		
		public InventoryComponent()
		{
			Permission = new ListenerProperty<InventoryPermission>(value => permission = value, () => permission);
			
			Available = new ReadonlyProperty<Inventory>(
				value => available = value,
				() => available,
				out availableListener
			);
			Forbidden = new ReadonlyProperty<Inventory>(
				value => forbidden = value,
				() => forbidden,
				out forbiddenListener
			);
			ReservedCapacity = new ReadonlyProperty<InventoryCapacity>(
				value => reservedCapacity = value,
				() => reservedCapacity,
				out reservedCapacityListener
			);
			AvailableCapacity = new ReadonlyProperty<InventoryCapacity>(
				value => availableCapacity = value,
				() => availableCapacity,
				out availableCapacityListener
			);
			AvailableWithoutDesire = new ReadonlyProperty<Inventory>(
				value => availableWithoutDesire = value,
				() => availableWithoutDesire,
				out availableWithoutDesireListener
			);

			Desired = new ListenerProperty<InventoryDesire>(value => desired = value, () => desired);
			Desired.ChangedSource += OnDesired;
		}

		public override bool Add(Inventory inventory) => Add(inventory, out _);

		public override bool Add(
			Inventory inventory,
			out Inventory overflow
		)
		{
			overflow = Inventory.Empty;
			if (inventory.IsEmpty) return false;
			
			// TODO: I suspect this should specify Available.Value as inventory0...
			var hasOverflow = AllCapacity.Value.AddClamped(
				All.Value,
				inventory,
				out var allReplacement,
				out overflow
			);

			AllListener.Value = allReplacement;
			Recalculate();

			return hasOverflow;
		}

		public override bool Remove(Inventory inventory) => Remove(inventory, out _);
		
		public override bool Remove(
			Inventory inventory,
			out Inventory overflow
		)
		{
			overflow = Inventory.Empty;
			if (inventory.IsEmpty) return false;

			var hasIntersection = All.Value.Intersects(
				inventory,
				out var maximumAvailableForRemoval
			);

			if (!hasIntersection)
			{
				overflow = inventory;
				return false;
			}

			overflow = inventory - maximumAvailableForRemoval;

			AllListener.Value -= maximumAvailableForRemoval;
			Recalculate();

			return !overflow.IsEmpty;
		}
		
		public void AddForbidden(Inventory inventory)
		{
			if (inventory.IsEmpty) return;
			if (!Available.Value.Contains(inventory)) Debug.LogError("Must forbid available items");
			forbiddenListener.Value += inventory;
			Recalculate();
		}
		
		public void RemoveForbidden(Inventory inventory)
		{
			if (inventory.IsEmpty) return;
			if (!Forbidden.Value.Contains(inventory)) Debug.LogError("Must make available already forbidden items");
				
			forbiddenListener.Value -= inventory;
			Recalculate();
		}

		public void AddReserved(Inventory inventory)
		{
			if (inventory.IsEmpty) return;
			if (!AvailableCapacity.Value.GetCapacityFor(Available.Value).Contains(inventory)) Debug.LogError("Must reserve available capacity");

			switch (ReservedCapacity.Value.Clamping)
			{
				case InventoryCapacity.Clamps.TotalWeight:
					reservedCapacityListener.Value = InventoryCapacity.ByTotalWeight(ReservedCapacity.Value.GetMaximum().TotalWeight + inventory.TotalWeight);
					break;
				case InventoryCapacity.Clamps.IndividualWeight:
					reservedCapacityListener.Value = InventoryCapacity.ByIndividualWeight(ReservedCapacity.Value.GetMaximum() + inventory);
					break;
				default:
					Debug.LogError("Unrecognized clamping: "+ReservedCapacity.Value.Clamping);
					break;
			}
			
			Recalculate();
		}

		public void RemoveReserved(Inventory inventory)
		{
			if (inventory.IsEmpty) return;
			if (!ReservedCapacity.Value.GetMaximum().Contains(inventory)) Debug.LogError("Must remove already reserved capacity");

			switch (ReservedCapacity.Value.Clamping)
			{
				case InventoryCapacity.Clamps.TotalWeight:
					reservedCapacityListener.Value = InventoryCapacity.ByTotalWeight(ReservedCapacity.Value.GetMaximum().TotalWeight - inventory.TotalWeight);
					break;
				case InventoryCapacity.Clamps.IndividualWeight:
					reservedCapacityListener.Value = InventoryCapacity.ByIndividualWeight(ReservedCapacity.Value.GetMaximum() - inventory);
					break;
				default: throw new ArgumentOutOfRangeException();
			}
			
			Recalculate();
		}
		
		#region Transactions
		bool TryRequestDeliver(
			Inventory inventory,
			out InventoryTransaction transaction,
			out Inventory overflow
		)
		{
			transaction = default;
			AvailableCapacity.Value.AddClamped(
				Available.Value,
				inventory,
				out _,
				out overflow
			);

			inventory -= overflow;
			
			if (inventory.IsEmpty) return false;
			
			AddReserved(inventory);
			
			transaction = InventoryTransaction.New(
				InventoryTransaction.Types.Deliver,
				this,
				inventory
			);

			return true;
		}

		#endregion
		
		public void Reset(
			InventoryPermission permission
		)
		{
			ResetId();
			
			Permission.Value = permission;
			Debug.LogError("TODO: More reset logic");
		}

		public override string ToString()
		{
			var result = "Inventory Component [ " + ShortId + " ]:";
			return result;
		}

		#region Utility
		void Recalculate()
		{
			
		}
		#endregion
	}
}