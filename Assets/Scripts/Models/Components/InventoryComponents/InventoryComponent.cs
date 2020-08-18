using System;
using System.Linq;
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
		InventoryComponent SalvageInventory { get; }
	}

	public class InventoryComponent : BaseInventoryComponent
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

		public void RemoveReserved(Inventory inventory)
		{
			if (inventory.IsEmpty) return;
			if (!ReservedCapacity.Value.GetMaximum().Contains(inventory)) Debug.LogError("Must remove already reserved capacity");

			switch (ReservedCapacity.Value.Clamping)
			{
				case InventoryCapacity.Clamps.Unlimited:
					return;
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
		public InventoryTransaction RequestDeliver(Inventory inventory)
		{
			var isValid = TryRequestDeliver(
				inventory,
				out var transaction,
				out var overflow
			);

			if (!isValid)
			{
				Debug.Break();
				throw new Exception(nameof(RequestDeliver) + " transaction invalid on " + ShortId + " for items:\n" + inventory);
			}
			if (!overflow.IsEmpty) throw new Exception(nameof(RequestDeliver) + " transaction returned overflow on " + ShortId + " for items:\n" + inventory);

			return transaction;
		}
		
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
		
		public void CompleteDeliver(
			InventoryTransaction transaction,
			bool addIntersection = true
		)
		{
			var isValid = TryCompleteDeliver(
				transaction,
				out var overflow,
				addIntersection
			);

			if (!isValid) throw new Exception(nameof(CompleteDeliver) + " transaction invalid on " + ShortId);
			if (!overflow.IsEmpty) throw new Exception(nameof(CompleteDeliver) + " transaction returned overflow on " + ShortId);
		}

		bool TryCompleteDeliver(
			InventoryTransaction transaction,
			out Inventory overflow,
			bool addIntersection = true
		)
		{
			var isIntersecting = ReservedCapacity.Value.GetMaximum().Intersects(
				transaction.Items,
				out var intersection
			);

			overflow = transaction.Items - intersection;

			if (!isIntersecting) return false;
			
			RemoveReserved(intersection);
			if (addIntersection) Add(intersection);
			return true;
		}
		
		public InventoryTransaction RequestDistribution(Inventory inventory)
		{
			var isValid = TryRequestDistribution(
				inventory,
				out var transaction,
				out var overflow
			);

			if (!isValid) throw new Exception(nameof(RequestDistribution) + " transaction invalid on " + ShortId + " for items:\n" + inventory);
			if (!overflow.IsEmpty) throw new Exception(nameof(RequestDistribution) + " transaction returned overflow on " + ShortId + " for items:\n" + inventory);

			return transaction;
		}

		bool TryRequestDistribution(
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
		
		public void CompleteDistribution(
			InventoryTransaction transaction,
			bool removeIntersection = true
		)
		{
			var isValid = TryCompleteDistribution(
				transaction,
				out var overflow,
				removeIntersection
			);

			if (!isValid) throw new Exception(nameof(CompleteDistribution) + " transaction invalid on " + ShortId);
			if (!overflow.IsEmpty) throw new Exception(nameof(CompleteDistribution) + " transaction returned overflow on " + ShortId);
		}
		
		bool TryCompleteDistribution(
			InventoryTransaction transaction,
			out Inventory overflow,
			bool removeIntersection = true
		)
		{
			var isIntersecting = Forbidden.Value.Intersects(
				transaction.Items,
				out var intersection
			);

			overflow = transaction.Items - intersection;

			if (!isIntersecting) return false;
			
			RemoveForbidden(intersection);
			if (removeIntersection) Remove(intersection);
			return true;
		}
		#endregion
		
		public void Reset(
			InventoryPermission permission,
			InventoryCapacity capacity,
			InventoryDesire? desired = null
		)
		{
			ResetId();
			
			Permission.Value = permission;
			
			AllListener.Value = Inventory.Empty;
			availableListener.Value = Inventory.Empty;
			forbiddenListener.Value = Inventory.Empty;
			AllCapacityListener.Value = capacity;
			availableCapacityListener.Value = capacity;

			Desired.SetValue(
				desired ?? InventoryDesire.Ignored(),
				this
			);
			
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
			var result = "Inventory Component [ " + ShortId + " ]:";
			var resultEntries = string.Empty;
			foreach (var itemType in Inventory.ValidTypes)
			{
				var allCapacityForItem = AllCapacity.Value.GetMaximumFor(itemType);
				
				if (allCapacityForItem == 0) continue;
				
				
				var allForItem = All.Value[itemType];
				var availableForItem = Available.Value[itemType];
				var forbiddenForItem = Forbidden.Value[itemType];
				var reservedForItem = ReservedCapacity.Value.GetMaximumFor(itemType);
				
				resultEntries += "\n - " + itemType;

				if (0 < allForItem || 0 < availableForItem || 0 < forbiddenForItem || 0 < reservedForItem)
				{
					resultEntries += "\n\tStored: \t\t\t" + All.Value[itemType];
					resultEntries += "\n\tAvailable: \t\t" + Available.Value[itemType];
					resultEntries += "\n\tForbidden: \t\t" + Forbidden.Value[itemType];
					resultEntries += "\n\tReserved Capacity: \t" + ReservedCapacity.Value.GetMaximumFor(itemType);
				}
				
				resultEntries += "\n\tStored Capacity: \t" + allCapacityForItem;
				
				var availableCapacityForItem = AvailableCapacity.Value.GetMaximumFor(itemType);

				if (allCapacityForItem != availableCapacityForItem) resultEntries += "\n\tAvailable Capacity: \t" + availableCapacityForItem;
			}

			result += (string.IsNullOrEmpty(resultEntries) ? "Empty" : resultEntries);

			string itemsToString(Inventory inventory)
			{
				if (inventory.IsEmpty) return "\t\tEmpty";

				var itemsToStringResult = string.Empty;

				foreach (var item in inventory.Entries.Where(e => 0 < e.Weight))
				{
					itemsToStringResult += "\n\t" + item.Type + "\t\t" + item.Weight;
				}

				return itemsToStringResult;
			}
			
			if (Desired.Value.AnyInventoriesNotEmpty)
			{
				result += "\n - Desired Storage:" + itemsToString(Desired.Value.Storage);
				result += "\n - Desired Delivery:" + itemsToString(Desired.Value.Delivery);
				result += "\n - Desired Distribution:" + itemsToString(Desired.Value.Distribution);
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
					if (capacityClampingNotMatched || AvailableCapacity.Value.GetMaximum().TotalWeight != CalculateAvailableCapacity().TotalWeight)
					{
						availableCapacityListener.Value = InventoryCapacity.ByTotalWeight(AllCapacity.Value.GetMaximum().TotalWeight);
					}
					break;
				case InventoryCapacity.Clamps.IndividualWeight:
					var currentAvailableCapacity = CalculateAvailableCapacity();
					if (capacityClampingNotMatched || AvailableCapacity.Value.GetMaximum() != currentAvailableCapacity)
					{
						availableCapacityListener.Value = InventoryCapacity.ByIndividualWeight(currentAvailableCapacity);	
					}
					break;
				default:
					Debug.LogError("Unrecognized clamping: "+AllCapacity.Value.Clamping);
					break;
			}

			if (Desired.Value.IsActive)
			{
				RecalculateDesired();

				if (Available.Value.Intersects(Desired.Value.Storage, out var availableStorageIntersection))
				{
					availableWithoutDesireListener.Value = Available.Value - availableStorageIntersection;
				}
				else availableWithoutDesireListener.Value = Available.Value;
			}
			else
			{
				availableWithoutDesireListener.Value = Available.Value;
			}
		}

		void RecalculateDesired()
		{
			var allIncoming = Available.Value + ReservedCapacity.Value.GetMaximum();
			
			Inventory desiredDelivery;
			Inventory desiredDistribution;

			if (Desired.Value.Storage.Intersects(allIncoming, out var deliveryIntersection))
			{
				desiredDelivery = Desired.Value.Storage - deliveryIntersection;
			}
			else
			{
				desiredDelivery = Desired.Value.Storage;
			}

			AllCapacity.Value
				.GetCapacityFor(All.Value)
				.Intersects(desiredDelivery, out desiredDelivery);

			if (Desired.Value.Storage.Intersects(Available.Value, out var availableDesiredIntersection))
			{
				desiredDistribution = Available.Value - availableDesiredIntersection;
			}
			else
			{
				desiredDistribution = Available.Value;
			}

			Desired.SetValue(
				InventoryDesire.New(
					Desired.Value.Storage,
					desiredDelivery,
					desiredDistribution
				),
				this
			);
		}

		Inventory CalculateAvailableCapacity() => AllCapacity.Value.GetMaximum() - (ReservedCapacity.Value.GetMaximum() + Forbidden.Value);
		#endregion
		
		#region Events
		void OnDesired(
			InventoryDesire desire,
			object source
		)
		{
			if (source != this) RecalculateDesired();
		}
		#endregion
	}
}