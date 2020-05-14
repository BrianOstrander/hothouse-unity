using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Lunra.Core;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
#pragma warning disable CS0661 // Defines == or != operator but does not override Object.GetHashCode()
#pragma warning disable CS0659 // Overrides Object.Equals(object) but does not override Object.GetHashCode()
	public struct Inventory
#pragma warning restore CS0659 // Overrides Object.Equals(object) but does not override Object.GetHashCode()
#pragma warning restore CS0661 // Defines == or != operator but does not override Object.GetHashCode()
	{
		public struct Entry
		{
			public readonly Item.Types Type;
			public readonly int Weight;

			public Entry(
				Item.Types type,
				int weight
			)
			{
				Type = type;
				Weight = weight;
			}
		}
		
		public static Inventory Empty => new Inventory(new Dictionary<Item.Types, int>());
		public static Inventory MaximumValue => new Inventory(
			EnumExtensions.GetValues(Item.Types.Unknown).ToDictionary(
				type => type,
				type => int.MaxValue
			)	
		);
		
		readonly ReadOnlyDictionary<Item.Types, int> entries;
		public readonly int TotalWeight;

		[JsonIgnore]
		public bool IsEmpty => 0 == TotalWeight;
		[JsonIgnore]
		public IEnumerable<(Item.Types Type, int Weight)> Entries
		{
			get
			{
				var current = this;
				return EnumExtensions.GetValues(Item.Types.Unknown)
					.Select(t => (t, current[t]));
			}
		}

		public Inventory(Dictionary<Item.Types, int> entries)
		{
			this.entries = new ReadOnlyDictionary<Item.Types, int>(entries);
			TotalWeight = entries.Select(kv => kv.Value).Sum();
		}

		[JsonIgnore]
		public int this[Item.Types type]
		{
			get
			{
				if (entries.TryGetValue(type, out var value)) return value;
				return 0;
			}
		}

		public bool Contains(Inventory inventory) => Entries.All(i => inventory[i.Type] <= i.Weight);

		public bool Intersects(Inventory inventory) => Intersects(inventory, out _);

		public bool Intersects(
			Inventory inventory,
			out Inventory intersection
		)
		{
			intersection = new Inventory(
				Entries.ToDictionary(
					i => i.Type,
					i => Mathf.Min(i.Weight, inventory[i.Type])
				)
			);
			
			return !intersection.IsEmpty;
		}
		
		public static Inventory Maximum(Inventory inventory0, Inventory inventory1)
		{
			if (inventory0 <= inventory1) return inventory1;
			return inventory0;
		}

		public static Inventory Minimum(Inventory inventory0, Inventory inventory1)
		{
			if (inventory0 <= inventory1) return inventory0;
			return inventory1;
		}

		#region Operator Overrides
		public static Inventory operator +(Inventory inventory0, Inventory inventory1)
		{
			return new Inventory(
				inventory0.Entries.ToDictionary(
					i => i.Type,
					i => i.Weight + inventory1[i.Type]
				)	
			);
		}
		
		public static Inventory operator +(Inventory inventory, (Item.Types Type, int Weight) entry)
		{
			return new Inventory(
				inventory.Entries.ToDictionary(
					i => i.Type,
					i => i.Type == entry.Type ? i.Weight + entry.Weight : i.Weight
				)	
			);
		}

		public static Inventory operator -(Inventory inventory0, Inventory inventory1)
		{
			return new Inventory(
				inventory0.Entries.ToDictionary(
					i => i.Type,
					i => i.Weight - inventory1[i.Type]
				)	
			);
		}
		
		public static Inventory operator -(Inventory inventory, (Item.Types Type, int Weight) entry)
		{
			return new Inventory(
				inventory.Entries.ToDictionary(
					i => i.Type,
					i => i.Type == entry.Type ? i.Weight - entry.Weight : i.Weight
				)	
			);
		}
		
		public static Inventory operator *(Inventory inventory0, Inventory inventory1)
		{
			return new Inventory(
				inventory0.Entries.ToDictionary(
					i => i.Type,
					i => i.Weight * inventory1[i.Type]
				)
			);
		}
		
		public static Inventory operator *(Inventory inventory, int weight)
		{
			return new Inventory(
				inventory.Entries.ToDictionary(
					i => i.Type,
					i => i.Weight * weight
				)	
			);
		}
		
		public static Inventory operator *(Inventory inventory, (Item.Types Type, int Weight) entry)
		{
			return new Inventory(
				inventory.Entries.ToDictionary(
					i => i.Type,
					i => i.Type == entry.Type ? i.Weight * entry.Weight : i.Weight
				)	
			);
		}

		public static bool operator <(Inventory inventory0, Inventory inventory1)
		{
			return inventory0.TotalWeight < inventory1.TotalWeight;
		}

		public static bool operator >(Inventory inventory0, Inventory inventory1)
		{
			return inventory0.TotalWeight > inventory1.TotalWeight;
		}

		public static bool operator <=(Inventory inventory0, Inventory inventory1)
		{
			if (inventory0 < inventory1) return true;
			if (inventory0 == inventory1) return true;
			return false;
		}

		public static bool operator >=(Inventory inventory0, Inventory inventory1)
		{
			if (inventory0 > inventory1) return true;
			if (inventory0 == inventory1) return true;
			return false;
		}

		public bool Equals(Inventory inventory)
		{
			if (TotalWeight != inventory.TotalWeight) return false;
			return Entries.All(i => i.Weight == inventory[i.Type]);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;

			return obj.GetType() == GetType() && Equals((Inventory)obj);
		}

		public static bool operator ==(Inventory inventory0, Inventory inventory1)
		{
			if (Equals(inventory0, inventory1)) return true;
			if (Equals(inventory0, null)) return false;
			if (Equals(inventory1, null)) return false;
			return inventory0.Equals(inventory1);
		}

		public static bool operator !=(Inventory inventory0, Inventory inventory1) { return !(inventory0 == inventory1); }
		#endregion

		public override string ToString()
		{
			if (TotalWeight == 0) return "Empty";
			
			var result = "[";
			foreach (var kv in entries.Where(kv => kv.Value != 0))
			{
				result += "\n\t" + kv.Key + " : " + kv.Value;
			}
			
			result += "\n]";
			return result + "\nWeight : " + TotalWeight;
		}
	}
}