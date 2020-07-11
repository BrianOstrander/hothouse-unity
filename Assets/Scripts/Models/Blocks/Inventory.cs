using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Lunra.Core;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Assertions;

namespace Lunra.Hothouse.Models
{
#pragma warning disable CS0661 // Defines == or != operator but does not override Object.GetHashCode()
#pragma warning disable CS0659 // Overrides Object.Equals(object) but does not override Object.GetHashCode()
	public struct Inventory
#pragma warning restore CS0659 // Overrides Object.Equals(object) but does not override Object.GetHashCode()
#pragma warning restore CS0661 // Defines == or != operator but does not override Object.GetHashCode()
	{
		public enum Types
		{
			Unknown = 0,
			
			StalkSeed = 10,
			StalkRaw = 11,
			StalkDry = 12,
			
			Rations = 20,
			Scrap = 30,
		}
		
		public static Inventory Empty => new Inventory(new Dictionary<Types, int>());
		public static Inventory MaximumValue => new Inventory(
			EnumExtensions.GetValues(Types.Unknown).ToDictionary(
				type => type,
				type => Int32.MaxValue
			)	
		);
		
		public static Inventory FromDictionary(Dictionary<Types, int> entries) => new Inventory(entries);
		public static Inventory FromEntry(Types type, int weight) => FromEntries((type, weight));
		public static Inventory FromEntries(params (Types Type, int Weight)[] entries)
		{
			return FromDictionary(
				entries.ToDictionary(
					entry => entry.Type,
					entry => entry.Weight
				)
			);
		}
		
		public static Types[] ValidTypes = EnumExtensions.GetValues(Types.Unknown);
		
		[JsonProperty] readonly ReadOnlyDictionary<Types, int> entries;
		public readonly int TotalWeight;

		[JsonIgnore]
		public bool IsEmpty => 0 == TotalWeight;
		[JsonIgnore]
		public IEnumerable<(Types Type, int Weight)> Entries
		{
			get
			{
				var current = this;
				return EnumExtensions.GetValues(Types.Unknown)
					.Select(t => (t, current[t]));
			}
		}

		public Inventory(Dictionary<Types, int> entries)
		{
			this.entries = new ReadOnlyDictionary<Types, int>(entries);

			try { TotalWeight = entries.Select(kv => kv.Value).Sum(); }
			catch (OverflowException) { TotalWeight = int.MaxValue; }
			
			Assert.IsTrue(
				entries.None(e => e.Value < 0),
				nameof(entries)+" should never contain values less than zero\n"+ToString(true)
			);
		}

		[JsonIgnore]
		public int this[Types type]
		{
			get
			{
				if (entries == null) return 0; // TODO: Why when serializing does this get called and trigger an error?
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
		
		public static Inventory operator +(Inventory inventory, (Types Type, int Weight) entry)
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
		
		public static Inventory operator -(Inventory inventory, (Types Type, int Weight) entry)
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
		
		public static Inventory operator *(Inventory inventory, float weight)
		{
			return new Inventory(
				inventory.Entries.ToDictionary(
					i => i.Type,
					i => Mathf.FloorToInt(i.Weight * weight)
				)	
			);
		}
		
		public static Inventory operator /(Inventory inventory, int weight)
		{
			return new Inventory(
				inventory.Entries.ToDictionary(
					i => i.Type,
					i => i.Weight / weight
				)	
			);
		}
		
		public static Inventory operator /(Inventory inventory, float weight)
		{
			return new Inventory(
				inventory.Entries.ToDictionary(
					i => i.Type,
					i => Mathf.FloorToInt(i.Weight / weight)
				)	
			);
		}
		
		public static Inventory operator *(Inventory inventory, (Types Type, int Weight) entry)
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

		public override string ToString() => ToString(false);
		
		public string ToString(bool ignoreWeight)
		{
			if (!ignoreWeight && TotalWeight == 0) return "Empty";
			
			var result = "[";
			foreach (var kv in entries.Where(kv => kv.Value != 0))
			{
				result += "\n\t" + kv.Key + " : " + kv.Value;
			}
			
			result += "\n]";
			return result + "\nWeight : " + TotalWeight;
		}
	}

	public static class InventoryExtensions
	{
		public static Inventory Sum(this IEnumerable<Inventory> entries)
		{
			var result = Inventory.Empty;
			foreach (var entry in entries) result += entry;
			return result;
		}

		public static Inventory ToInventory(this (Inventory.Types Type, int Weight) entry) => Inventory.FromEntry(entry.Type, entry.Weight);
	}
}