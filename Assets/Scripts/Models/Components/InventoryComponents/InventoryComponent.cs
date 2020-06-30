using System;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public interface IInventoryModel : IBaseInventoryModel, IEnterableModel
	{
		InventoryComponent Inventory { get; }
	}
	
	public interface IConstructionModel : IBaseInventoryModel, IEnterableModel
	{
		InventoryComponent ConstructionInventory { get; }
	}

	public class InventoryComponent : BaseInventoryComponent
	{
		#region Serialized
		[JsonProperty] InventoryPermission permission = InventoryPermission.AllForAnyJob();
		[JsonProperty] public ListenerProperty<InventoryPermission> Permission { get; }

		[JsonProperty] Inventory available = Inventory.Empty;
		readonly ListenerProperty<Inventory> availableListener;
		[JsonIgnore] public ReadonlyProperty<Inventory> Available { get; }
		
		[JsonProperty] Inventory forbidden = Inventory.Empty;
		readonly ListenerProperty<Inventory> forbiddenListener;
		[JsonIgnore] public ReadonlyProperty<Inventory> Forbidden { get; }

		[JsonProperty] InventoryCapacity reservedCapacity = InventoryCapacity.None();
		readonly ListenerProperty<InventoryCapacity> reservedCapacityListener;
		[JsonIgnore] public ReadonlyProperty<InventoryCapacity> ReservedCapacity { get; }

		[JsonProperty] InventoryCapacity availableCapacity = InventoryCapacity.None();
		readonly ListenerProperty<InventoryCapacity> availableCapacityListener;
		[JsonIgnore] public ReadonlyProperty<InventoryCapacity> AvailableCapacity { get; }
		#endregion
		
		#region Non Serialized
		#endregion

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
		}

		public override bool Add(Inventory inventory) => Add(inventory, out _);

		public override bool Add(
			Inventory inventory,
			out Inventory overflow
		)
		{
			overflow = Inventory.Empty;
			if (inventory.IsEmpty) return false;
			
			var hasOverflow = AvailableCapacity.Value.AddClamped(
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
		
		public InventoryComponent RemoveForbidden(Inventory inventory)
		{
			if (inventory.IsEmpty) return this;
			if (!Forbidden.Value.Contains(inventory)) Debug.LogError("Must make available already forbidden items");
				
			forbiddenListener.Value -= inventory;
			Recalculate();
			
			return this;
		}

		public void AddReserved(Inventory inventory)
		{
			if (inventory.IsEmpty) return;
			if (!AvailableCapacity.Value.GetCapacityFor(Available.Value).Contains(inventory)) Debug.LogError("Must reserve available capacity");

			switch (ReservedCapacity.Value.Clamping)
			{
				case InventoryCapacity.Clamps.Unlimited:
					return;
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

		public InventoryComponent RemoveReserved(Inventory inventory)
		{
			if (inventory.IsEmpty) return this;
			if (!ReservedCapacity.Value.GetMaximum().Contains(inventory)) Debug.LogError("Must remove already reserved capacity");

			switch (ReservedCapacity.Value.Clamping)
			{
				case InventoryCapacity.Clamps.Unlimited:
					return this;
				case InventoryCapacity.Clamps.TotalWeight:
					reservedCapacityListener.Value = InventoryCapacity.ByTotalWeight(ReservedCapacity.Value.GetMaximum().TotalWeight - inventory.TotalWeight);
					break;
				case InventoryCapacity.Clamps.IndividualWeight:
					reservedCapacityListener.Value = InventoryCapacity.ByIndividualWeight(ReservedCapacity.Value.GetMaximum() - inventory);
					break;
				default: throw new ArgumentOutOfRangeException();
			}
			
			Recalculate();
			
			return this;
		}
		
		#region Transactions
		public bool RequestDeliver(
			Inventory inventory,
			out InventoryTransaction transaction,
			out Inventory overflow
		)
		{
			transaction = default;
			AvailableCapacity.Value.AddClamped(
				All.Value,
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

		public void CompleteDeliver(
			InventoryTransaction transaction
		)
		{
			RemoveReserved(transaction.Items);
			Add(transaction.Items);
		}

		public bool RequestDistribution(
			Inventory inventory,
			out InventoryTransaction transaction,
			out Inventory overflow
		)
		{
			transaction = default;

			var isIntersecting = Available.Value.Intersects(
				inventory,
				out var intersection
			);

			overflow = inventory - intersection;

			if (!isIntersecting) return false;

			inventory -= overflow;
			
			AddForbidden(inventory);
			
			transaction = InventoryTransaction.New(
				InventoryTransaction.Types.Distribute,
				this,
				inventory
			);
			
			return true;
		}
		#endregion
		
		public void Reset(
			InventoryPermission permission,
			InventoryCapacity capacity
		)
		{
			ResetId();
			
			Permission.Value = permission;
			
			AllListener.Value = Inventory.Empty;
			availableListener.Value = Inventory.Empty;
			forbiddenListener.Value = Inventory.Empty;
			AllCapacityListener.Value = capacity;
			availableCapacityListener.Value = capacity;

			switch (capacity.Clamping)
			{
				case InventoryCapacity.Clamps.None:
					reservedCapacityListener.Value = InventoryCapacity.None();
					break;
				case InventoryCapacity.Clamps.Unlimited:
					reservedCapacityListener.Value = InventoryCapacity.Unlimited();		
					break;
				case InventoryCapacity.Clamps.TotalWeight:
					reservedCapacityListener.Value = InventoryCapacity.ByTotalWeight(0);
					break;
				case InventoryCapacity.Clamps.IndividualWeight:
					reservedCapacityListener.Value = InventoryCapacity.ByIndividualWeight(Inventory.Empty);
					break;
				default:
					Debug.LogError("Unrecognized clamping: "+capacity.Clamping);
					break;
			}

			Recalculate();
		}

		public override string ToString()
		{
			var result = "Inventory Component:\n";
			foreach (var itemType in Inventory.ValidTypes)
			{
				result += "\n - " + itemType;
				result += "\n\tStored: \t\t\t" + All.Value[itemType];
				result += "\n\tAvailable: \t\t" + Available.Value[itemType];
				result += "\n\tForbidden: \t\t" + Forbidden.Value[itemType];
				result += "\n\tReserved Capacity: \t" + ReservedCapacity.Value.GetMaximumFor(itemType);
				result += "\n\tStored Capacity: \t" + AllCapacity.Value.GetMaximumFor(itemType);
				result += "\n\tAvailable Capacity: \t" + AvailableCapacity.Value.GetMaximumFor(itemType);
				result += "\n";
			}

			return result;
		}

		#region Utility
		void Recalculate()
		{
			var currentAvailable = All.Value - Forbidden.Value;

			if (currentAvailable != Available.Value) availableListener.Value = currentAvailable;

			var capacityClampingNotMatched = AvailableCapacity.Value.Clamping != AllCapacity.Value.Clamping; 
			
			switch (AllCapacity.Value.Clamping)
			{
				case InventoryCapacity.Clamps.None:
				case InventoryCapacity.Clamps.Unlimited:
					if (capacityClampingNotMatched)
					{
						availableCapacityListener.Value = AllCapacity.Value;
					}
					break;
				case InventoryCapacity.Clamps.TotalWeight:
					if (capacityClampingNotMatched || AvailableCapacity.Value.GetMaximum().TotalWeight != (AllCapacity.Value.GetMaximum() - ReservedCapacity.Value.GetMaximum()).TotalWeight)
					{
						availableCapacityListener.Value = InventoryCapacity.ByTotalWeight(AllCapacity.Value.GetMaximum().TotalWeight);
					}
					break;
				case InventoryCapacity.Clamps.IndividualWeight:
					var currentAvailableCapacity = AllCapacity.Value.GetMaximum() - ReservedCapacity.Value.GetMaximum();

					if (capacityClampingNotMatched || currentAvailableCapacity != AvailableCapacity.Value.GetMaximum())
					{
						availableCapacityListener.Value = InventoryCapacity.ByIndividualWeight(currentAvailableCapacity);	
					}
					break;
				default:
					Debug.LogError("Unrecognized clamping: "+AllCapacity.Value.Clamping);
					break;
			}
		}
		#endregion
	}
}