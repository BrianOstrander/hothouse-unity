using System;
using System.Linq;
using UnityEngine;

namespace Lunra.WildVacuum.Models
{
	public struct Inventory
	{
		public static Inventory Empty { get; } = new Inventory(Item.Empty, Item.Empty);

		public static Inventory Populate(
			Func<Item.Types, int> capacityPredicate,
			Func<Item.Types, int> currentPredicate
		)
		{
			return new Inventory(
				Item.Populate(capacityPredicate),
				Item.Populate(currentPredicate)
			);
		}

		public readonly Item[] Maximum;
		public readonly Item[] Current;
		
		Inventory(
			Item[] maximum,
			Item[] current
		)
		{
			if (maximum.Length != current.Length) throw new Exception("Capacity and entries must have the same length");
			
			Maximum = maximum;
			Current = current;
		}

		public Inventory SetMaximum(int count, Item.Types type) => SetMaximum(current => current.Type == type ? count : current.Count);
		
		public Inventory SetMaximum(Item item) => SetMaximum(current => current.Type == item.Type ? item.Count : current.Count);
		
		public Inventory SetMaximum(Func<Item, int> predicate)
		{
			return new Inventory(
				Maximum.Select(i => Item.New(Mathf.Max(0, predicate(i)), i.Type)).ToArray(),
				Current.Select(i => Item.New(Mathf.Min(i.Count, predicate(i)), i.Type)).ToArray()
			);
		}
		
		public Inventory SetCurrent(int count, Item.Types type) => SetCurrent(current => current.Type == type ? count : current.Count);
		
		public Inventory SetCurrent(Item item) => SetCurrent(current => current.Type == item.Type ? item.Count : current.Count);
		
		public Inventory SetCurrent(Func<Item, int> predicate)
		{
			Func<Item.Types, int> getMaximum = GetMaximum;
			return new Inventory(
				Maximum,
				Current.Select(
					i => Item.New(
						Mathf.Clamp(predicate(i), 0, getMaximum(i.Type)),
						i.Type
					)
				).ToArray()
			);
		}
		
		public Inventory Add(int count, Item.Types type) => Add(current => current.Type == type ? count : current.Count);
		
		public Inventory Add(Item item) => Add(current => current.Type == item.Type ? item.Count : current.Count);
		
		public Inventory Add(Func<Item, int> predicate)
		{
			Func<Item.Types, int> getMaximum = GetMaximum;
			return new Inventory(
				Maximum,
				Current.Select(
					i => Item.New(
						Mathf.Clamp(i.Count + predicate(i), 0, getMaximum(i.Type)),
						i.Type
					)
				).ToArray()
			);
		}
		
		public Inventory Subtract(int count, Item.Types type) => Subtract(current => current.Type == type ? count : current.Count);
		
		public Inventory Subtract(Item item) => Subtract(current => current.Type == item.Type ? item.Count : current.Count);
		
		public Inventory Subtract(Func<Item, int> predicate)
		{
			Func<Item.Types, int> getMaximum = GetMaximum;
			return new Inventory(
				Maximum,
				Current.Select(
					i => Item.New(
						Mathf.Clamp(i.Count - predicate(i), 0, getMaximum(i.Type)),
						i.Type
					)
				).ToArray()
			);
		}

		public int GetMaximum(Item.Types type) => Maximum.FirstOrDefault(i => i.Type == type).Count;
		
		public int GetCurrent(Item.Types type) => Current.FirstOrDefault(i => i.Type == type).Count;

		public int GetCapacity(Item.Types type) => GetMaximum(type) - GetCurrent(type);

		public int this[Item.Types type] => GetCurrent(type);

		public override string ToString()
		{
			var result = "[";

			result += "\n\tType\t{ Curr\t:\tMax }";
			
			foreach (var entry in Current) result += "\n\t" + entry.Type + "\t{ " + entry.Count + "\t:\t" + GetMaximum(entry.Type) + " }";
			
			return result + "\n]";
		}
	}
}