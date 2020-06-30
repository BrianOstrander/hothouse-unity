using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Lunra.Core;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public struct InventoryCapacity
	{
		public enum Clamps
		{
			Unknown = 0,
			None = 10,
			Unlimited = 20,
			TotalWeight = 30,
			IndividualWeight = 40
		}
		
		public static InventoryCapacity None() => new InventoryCapacity(
			Clamps.None,
			0,
			Inventory.Empty
		);
		
		public static InventoryCapacity Unlimited() => new InventoryCapacity(
			Clamps.Unlimited,
			0,
			Inventory.Empty
		);
		
		public static InventoryCapacity ByTotalWeight(int weightMaximum) => new InventoryCapacity(
			Clamps.TotalWeight,
			weightMaximum,
			Inventory.Empty
		);
		
		public static InventoryCapacity ByIndividualWeight(Inventory inventoryMaximum) => new InventoryCapacity(
			Clamps.IndividualWeight,
			0,
			inventoryMaximum
		);

		public readonly Clamps Clamping;
		[JsonProperty] readonly int weightMaximum;
		[JsonProperty] readonly Inventory inventoryMaximum;

		InventoryCapacity(
			Clamps clamping,
			int weightMaximum,
			Inventory inventoryMaximum
		)
		{
			Clamping = clamping;
			this.weightMaximum = weightMaximum;
			this.inventoryMaximum = inventoryMaximum;
		}

		public bool IsFull(Inventory inventory)
		{
			switch (Clamping)
			{
				case Clamps.None:
					return true;
				case Clamps.Unlimited:
					return false;
				case Clamps.TotalWeight:
					return weightMaximum <= inventory.TotalWeight;
				case Clamps.IndividualWeight:
					return inventoryMaximum.Entries.All(i => i.Weight <= inventory[i.Type]);
				default:
					Debug.LogError("Unrecognized clamp: "+Clamping);
					return true;
			}
		}
		
		public bool IsNotFull(Inventory inventory) => !IsFull(inventory);

		public bool HasCapacityFor(
			Inventory inventory0,
			Inventory inventory1
		)
		{
			foreach (var entry in inventory1.Entries)
			{
				if (entry.Weight == 0) continue;
				if (HasCapacityFor(inventory0, entry.Type)) return true;
			}

			return false;
		}

		public bool HasCapacityFor(
			Inventory inventory,
			Inventory.Types type
		)
		{
			return 0 < GetCapacityFor(inventory, type);
		}
		
		public bool HasCapacityFor(
			Inventory inventory,
			Inventory.Types type,
			out int weight
		)
		{
			return 0 < (weight = GetCapacityFor(inventory, type));
		}
		
		public bool HasCapacityFor(
			Inventory inventory,
			(Inventory.Types Type, int Weight) entry
		)
		{
			return entry.Weight <= GetCapacityFor(inventory, entry.Type);
		}

		public bool HasCapacityFor(
			Inventory inventory,
			(Inventory.Types Type, int Weight) entry,
			out int weight
		)
		{
			return entry.Weight <= (weight = GetCapacityFor(inventory, entry.Type));
		}

		public Inventory GetMaximum()
		{
			switch (Clamping)
			{
				case Clamps.None:
					return Inventory.Empty;
				case Clamps.Unlimited:
					return Inventory.MaximumValue;
				case Clamps.IndividualWeight:
					return inventoryMaximum;
				case Clamps.TotalWeight:
					var currentWeightMaximum = weightMaximum;
					return new Inventory(
						EnumExtensions.GetValues(Inventory.Types.Unknown).ToDictionary(
							type => type,
							type => currentWeightMaximum
						)	
					);
				default:
					Debug.LogError("Unrecognized clamp: "+Clamping);
					return Inventory.MaximumValue;
			}
		}

		public int GetMaximumFor(Inventory.Types type)
		{
			switch (Clamping)
			{
				case Clamps.None:
					return 0;
				case Clamps.Unlimited:
					return int.MaxValue;
				case Clamps.IndividualWeight:
					return inventoryMaximum[type];
				case Clamps.TotalWeight:
					return weightMaximum;
				default:
					Debug.LogError("Unrecognized clamp: "+Clamping);
					return int.MaxValue;
			}
		}
		
		/// <summary>
		/// Gets the remaining capacity assuming the inventory is already filled with the specified items.
		/// </summary>
		/// <remarks>No negative inventories are returned by this.</remarks>
		/// <param name="inventory"></param>
		/// <returns>All items that could be added to the inventory without going over the capacity.</returns>
		public Inventory GetCapacityFor(Inventory inventory)
		{
			switch (Clamping)
			{
				case Clamps.None:
					return Inventory.Empty;
				case Clamps.Unlimited:
					return inventory - Inventory.MaximumValue;
				case Clamps.TotalWeight:
					var weightRemaining = weightMaximum - inventory.TotalWeight;
					if (weightRemaining <= 0) return Inventory.Empty;
					return new Inventory(
						EnumExtensions.GetValues(Inventory.Types.Unknown).ToDictionary(
							type => type,
							type => weightRemaining
						)	
					);
				case Clamps.IndividualWeight:
					var individualWeightResult = new Dictionary<Inventory.Types, int>();
					foreach (var entryMaximum in inventoryMaximum.Entries)
					{
						individualWeightResult.Add(entryMaximum.Type, Mathf.Max(0, entryMaximum.Weight - inventory[entryMaximum.Type]));
					}
					return new Inventory(individualWeightResult); 
				default:
					Debug.LogError("Unrecognized clamp: "+Clamping);
					return inventory;
			}
		}
		
		/// <summary>
		/// Gets the remaining capacity assuming the inventory is already filled with the specified items.
		/// </summary>
		/// <remarks>No negative inventories are returned by this.</remarks>
		/// <param name="inventory"></param>
		/// <param name="type"></param>
		/// <returns>
		/// Number of items of the specified type that could be added to the inventory without going over capacity.
		/// </returns>
		public int GetCapacityFor(
			Inventory inventory,
			Inventory.Types type
		)
		{
			switch (Clamping)
			{
				case Clamps.None:
					return 0;
				case Clamps.Unlimited:
					return int.MaxValue - inventory[type];
				case Clamps.TotalWeight:
					return Mathf.Max(0, weightMaximum - inventory.TotalWeight);
				case Clamps.IndividualWeight:
					return Mathf.Max(0, inventoryMaximum[type] - inventory[type]); 
				default:
					Debug.LogError("Unrecognized clamp: "+Clamping);
					return int.MaxValue;
			}
		}

		public bool GetClamped(
			Inventory inventory,
			out Inventory clamped 
		)
		{
			return GetClamped(
				inventory,
				out clamped,
				out _
			);
		}
		
		public bool GetClamped(
			Inventory inventory,
			out Inventory clamped,
			out Inventory overflow
		)
		{
			switch (Clamping)
			{
				case Clamps.None:
					clamped = Inventory.Empty;
					overflow = inventory;
					return !overflow.IsEmpty;
				case Clamps.Unlimited:
					clamped = inventory;
					overflow = Inventory.Empty;
					return false;
			}

			var clampedResult = new Dictionary<Inventory.Types, int>();
			var overflowResult = new Dictionary<Inventory.Types, int>();

			switch (Clamping)
			{
				case Clamps.TotalWeight:
					var currentWeight = 0;
					foreach (var entry in inventory.Entries)
					{
						var weightRemaining = weightMaximum - currentWeight;
						if (0 < weightRemaining)
						{
							var weightToAdd = Mathf.Min(entry.Weight, weightRemaining);
							clampedResult.Add(entry.Type, weightToAdd);
							overflowResult.Add(entry.Type, Mathf.Max(0, entry.Weight - weightToAdd));
							currentWeight += weightToAdd;
						}
						else overflowResult.Add(entry.Type, entry.Weight);
					}
					break;
				case Clamps.IndividualWeight:
					foreach (var entry in inventory.Entries)
					{
						var currentWeightMaximum = inventoryMaximum[entry.Type];
						clampedResult.Add(entry.Type, Mathf.Min(entry.Weight, currentWeightMaximum));
						overflowResult.Add(entry.Type, Mathf.Max(0, entry.Weight - currentWeightMaximum));
					}
					break;
				default:
					Debug.LogError("Unrecognized clamp: "+Clamping);
					clamped = inventory;
					overflow = Inventory.Empty;
					return false;
			}
			
			clamped = new Inventory(clampedResult);
			overflow = new Inventory(overflowResult);

			return !overflow.IsEmpty;
		}

		/// <summary>
		/// This will add two inventories, but prioritize the inventory0, so items will not be replaced with items from
		/// the inventory1.
		/// </summary>
		/// <param name="inventory0"></param>
		/// <param name="inventory1"></param>
		/// <param name="clamped"></param>
		/// <returns></returns>
		public bool AddClamped(
			Inventory inventory0,
			Inventory inventory1,
			out Inventory clamped
		)
		{
			return AddClamped(
				inventory0,
				inventory1,
				out clamped,
				out _
			);
		}
		
		/// <summary>
		/// This will add two inventories, but prioritize the inventory0, so items will not be replaced with items from
		/// the inventory1.
		/// </summary>
		/// <param name="inventory0"></param>
		/// <param name="inventory1"></param>
		/// <param name="clamped"></param>
		/// <param name="overflow"></param>
		/// <returns>true if clamping occured, otherwise false.</returns>
		public bool AddClamped(
			Inventory inventory0,
			Inventory inventory1,
			out Inventory clamped,
			out Inventory overflow
		)
		{
			switch (Clamping)
			{
				case Clamps.None:
					clamped = Inventory.Empty;
					overflow = inventory0 + inventory1;
					return !overflow.IsEmpty;
				case Clamps.Unlimited:
					clamped = inventory0 + inventory1;
					overflow = Inventory.Empty;
					return false;
			}
			
			var clampedResult = new Dictionary<Inventory.Types, int>();
			var overflowResult = new Dictionary<Inventory.Types, int>();

			switch (Clamping)
			{
				case Clamps.TotalWeight:
					var weightExtraRemaining = Mathf.Max(0, weightMaximum - inventory0.TotalWeight);
					foreach (var type in Inventory.ValidTypes)
					{
						if (0 < weightExtraRemaining)
						{
							var weightToAdd = inventory0[type];
							var weightToAddExtra = Mathf.Min(weightExtraRemaining, inventory1[type]);
							clampedResult.Add(type, weightToAdd + weightToAddExtra);
							overflowResult.Add(type, Mathf.Max(0, inventory1[type] - weightToAddExtra));
						}
						else
						{
							clampedResult.Add(type, inventory0[type]);
							overflowResult.Add(type, inventory1[type]);
						}
					}
					break;
				case Clamps.IndividualWeight:
					foreach (var type in Inventory.ValidTypes)
					{
						var currentWeightMaximum = inventoryMaximum[type];
						var weightToAdd = inventory0[type] + inventory1[type];
						clampedResult.Add(type, Mathf.Min(weightToAdd, currentWeightMaximum));
						overflowResult.Add(type, Mathf.Max(0, weightToAdd - currentWeightMaximum));
					}
					break;
				default:
					Debug.LogError("Unrecognized clamp: "+Clamping);
					clamped = inventory0 + inventory1;
					overflow = Inventory.Empty;
					return false;
			}
			
			clamped = new Inventory(clampedResult);
			overflow = new Inventory(overflowResult);

			return !overflow.IsEmpty;
		}

		public override string ToString()
		{
			var result = "Clamping : " + Clamping;
			switch (Clamping)
			{
				case Clamps.TotalWeight:
					result += "\nWeightMaximum : " + weightMaximum;
					break;
				case Clamps.IndividualWeight:
					result += "\nInventoryMaximum : " + inventoryMaximum;
					break;
			}

			return result;
		}
	}
}