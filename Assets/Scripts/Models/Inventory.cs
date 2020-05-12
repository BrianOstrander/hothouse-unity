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
		
		// TODO: Hide this and replace with something that returns (Item.Types Type, int Weight)
		// Really shouldn't show this since it may not contain all values!
		public readonly ReadOnlyDictionary<Item.Types, int> Entries;
		public readonly int Weight;

		public bool IsEmpty => 0 == Weight;

		public Inventory(Dictionary<Item.Types, int> entries)
		{
			// TODO: Filter out zero kv's
			Entries = new ReadOnlyDictionary<Item.Types, int>(entries);
			Weight = entries.Select(kv => kv.Value).Sum();
		}

		public int this[Item.Types type]
		{
			get
			{
				if (Entries.TryGetValue(type, out var value)) return value;
				return 0;
			}
		}

		public IEnumerable<Item.Types> Types => Entries.Keys;

		public bool Contains(Inventory inventory)
		{
			var current = this;
			return inventory.Types.All(t => inventory[t] <= current[t]);
		}

		public bool Intersects(Inventory inventory) => Intersects(inventory, out _);

		public bool Intersects(
			Inventory inventory,
			out Inventory intersection
		)
		{
			var current = this;
			intersection = new Inventory(
				inventory.Types.Intersect(current.Types).ToDictionary(
					type => type,
					type => Mathf.Min(current[type], inventory[type])
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
				inventory0.Types.Union(inventory1.Types).ToDictionary(
					type => type,
					type => inventory0[type] + inventory1[type]
				)
			);
		}
		
		public static Inventory operator +(Inventory inventory0, (Item.Types Type, int Weight) entry)
		{
			return new Inventory(
				inventory0.Types.Union(entry.Type).ToDictionary(
					type => type,
					type => type == entry.Type ? inventory0[type] + entry.Weight : inventory0[type]
				)
			);
		}

		public static Inventory operator -(Inventory inventory0, Inventory inventory1)
		{
			return new Inventory(
				inventory0.Types.Union(inventory1.Types).ToDictionary(
					type => type,
					type => inventory0[type] - inventory1[type]
				)
			);
		}
		
		public static Inventory operator -(Inventory inventory, (Item.Types Type, int Weight) entry)
		{
			return new Inventory(
				inventory.Types.Union(entry.Type).ToDictionary(
					type => type,
					type => type == entry.Type ? inventory[type] - entry.Weight : inventory[type]
				)
			);
		}
		
		public static Inventory operator *(Inventory inventory0, Inventory inventory1)
		{
			return new Inventory(
				inventory0.Types.Union(inventory1.Types).ToDictionary(
					type => type,
					type => inventory0[type] * inventory1[type]
				)
			);
		}
		
		public static Inventory operator *(Inventory inventory, int value)
		{
			return new Inventory(
					inventory.Types.ToDictionary(
					type => type,
					type => inventory[type] * value
				)
			);
		}
		
		public static Inventory operator *(Inventory inventory, (Item.Types Type, int Weight) entry)
		{
			return new Inventory(
				inventory.Types.ToDictionary(
					type => type,
					type => type == entry.Type ? inventory[type] * entry.Weight : inventory[type] 
				)
			);
		}

		public static bool operator <(Inventory inventory0, Inventory inventory1)
		{
			return inventory0.Weight < inventory1.Weight;
		}

		public static bool operator >(Inventory inventory0, Inventory inventory1)
		{
			return inventory0.Weight > inventory1.Weight;
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
			if (Weight != inventory.Weight) return false;
			var current = this;
			return Types.Union(inventory.Types).All(t => current[t] == inventory[t]);
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
	}
}