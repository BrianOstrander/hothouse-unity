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
			TotalWeight = 20,
			IndividualWeight = 30
		}
		
		public static InventoryCapacity ByNone() => new InventoryCapacity(
			Clamps.None,
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

		public bool IsNotFull(Inventory inventory)
		{
			switch (Clamping)
			{
				case Clamps.None:
					return true;
				case Clamps.TotalWeight:
					return inventory.Weight < weightMaximum;
				case Clamps.IndividualWeight:
					var current = this;
					return inventory.Entries.All(e => e.Value < current.inventoryMaximum[e.Key]);
				default:
					Debug.LogError("Unrecognized clamp: "+Clamping);
					return true;
			}
		}

		public bool IsFull(Inventory inventory) => !IsNotFull(inventory);

		public bool HasCapacityFor(
			Inventory inventory,
			Item.Types type
		)
		{
			return 0 < GetCapacityFor(inventory, type);
		}
		
		public bool HasCapacityFor(
			Inventory inventory,
			Item.Types type,
			out int weight
		)
		{
			return 0 < (weight = GetCapacityFor(inventory, type));
		}
		
		public bool HasCapacityFor(
			Inventory inventory,
			(Item.Types Type, int Weight) entry
		)
		{
			return entry.Weight <= GetCapacityFor(inventory, entry.Type);
		}

		public bool HasCapacityFor(
			Inventory inventory,
			(Item.Types Type, int Weight) entry,
			out int weight
		)
		{
			return entry.Weight <= (weight = Mathf.Min(entry.Weight, GetCapacityFor(inventory, entry.Type)));
		}
		
		public int GetCapacityFor(
			Inventory inventory,
			Item.Types type
		)
		{
			switch (Clamping)
			{
				case Clamps.None:
					return int.MaxValue;
				case Clamps.TotalWeight:
					return weightMaximum - inventory.Weight;
				case Clamps.IndividualWeight:
					return inventoryMaximum[type] - inventory[type]; 
				default:
					Debug.LogError("Unrecognized clamp: "+Clamping);
					return int.MaxValue;
			}
		}
		
		public int GetCapacityFor(
			Inventory inventory,
			(Item.Types Type, int Weight) entry
		)
		{
			switch (Clamping)
			{
				case Clamps.None:
					return entry.Weight;
				case Clamps.TotalWeight:
					return Mathf.Min(weightMaximum - inventory.Weight, entry.Weight);
				case Clamps.IndividualWeight:
					return Mathf.Min(inventoryMaximum[entry.Type] - inventory[entry.Type], entry.Weight); 
				default:
					Debug.LogError("Unrecognized clamp: "+Clamping);
					return entry.Weight;
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
			if (Clamping == Clamps.None)
			{
				clamped = inventory;
				overflow = Inventory.Empty;
				return false;
			}

			var clampedResult = new Dictionary<Item.Types, int>();
			var overflowResult = new Dictionary<Item.Types, int>();

			switch (Clamping)
			{
				case Clamps.TotalWeight:
					var currentWeight = 0;
					foreach (var entry in inventory.Entries)
					{
						var weightRemaining = weightMaximum - currentWeight;
						if (0 < weightRemaining)
						{
							var weightToAdd = Mathf.Min(entry.Value, weightRemaining);
							clampedResult.Add(entry.Key, weightToAdd);
							overflowResult.Add(entry.Key, entry.Value - weightToAdd);
							currentWeight += weightToAdd;
						}
						else overflowResult.Add(entry.Key, entry.Value);
					}
					break;
				case Clamps.IndividualWeight:
					foreach (var entry in inventory.Entries)
					{
						var currentWeightMaximum = inventoryMaximum[entry.Key];
						clampedResult.Add(entry.Key, Mathf.Min(entry.Value, currentWeightMaximum));
						overflowResult.Add(entry.Key, Mathf.Max(0, entry.Value - currentWeightMaximum));
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
		/// <returns></returns>
		public bool AddClamped(
			Inventory inventory0,
			Inventory inventory1,
			out Inventory clamped,
			out Inventory overflow
		)
		{
			if (Clamping == Clamps.None)
			{
				clamped = inventory0 + inventory1;
				overflow = Inventory.Empty;
				return false;
			}

			var clampedResult = new Dictionary<Item.Types, int>();
			var overflowResult = new Dictionary<Item.Types, int>();

			switch (Clamping)
			{
				case Clamps.TotalWeight:
					var weightExtraRemaining = Mathf.Max(0, weightMaximum - inventory0.Weight);
					foreach (var type in inventory0.Types.Union(inventory1.Types))
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
					foreach (var type in inventory0.Types.Union(inventory1.Types))
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
	}
}