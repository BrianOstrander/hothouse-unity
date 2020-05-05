using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
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
		
		public static Inventory Populate(
			Dictionary<Item.Types, int> all
		)
		{
			var result = Item.Populate(
				t =>
				{
					all.TryGetValue(t, out var count);
					return count;
				}
			);
			return new Inventory(result, result); 
		}
		
		public static Inventory PopulateMaximum(
			Dictionary<Item.Types, int> all
		)
		{
			var resultCurrent = Item.Populate(
				t =>
				{
					all.TryGetValue(t, out var count);
					return count;
				}
			);
			
			return new Inventory(resultCurrent, Item.Populate(t => 0)); 
		}

		public readonly Item[] Maximum;
		public readonly Item[] Current;
		public readonly bool IsEmpty;
		
		Inventory(
			Item[] maximum,
			Item[] current
		)
		{
			if (maximum.Length != current.Length) throw new Exception("Capacity and entries must have the same length");
			
			Maximum = maximum;
			Current = current;

			IsEmpty = !Current.Any(i => 0 < i.Count);
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
		
		public Inventory Add(
			Inventory target,
			out Inventory overflow
		)
		{
			var result = Add(i => target[i.Type]);

			var rawOverflow = new Dictionary<Item.Types, int>();
			
			foreach (var item in target.Current)
			{
				var capacity = GetCapacity(item.Type);
				if (capacity < item.Count) rawOverflow.Add(item.Type, item.Count - capacity);
			}
			
			overflow = Populate(rawOverflow);

			return result;
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

		public Item[] GetFull()
		{
			Func<Item.Types, int> getCurrent = GetCurrent;
			return Maximum.Where(m => m.Count != 0 && m.Count == getCurrent(m.Type)).ToArray();
		}
		
		public bool IsFull(Item.Types type) => GetCapacity(type) == 0;

		public bool IsNoneFull() => !IsAnyFull();
		
		public bool IsAnyFull()
		{
			foreach (var type in EnumExtensions.GetValues(Item.Types.Unknown))
			{
				if (IsFull(type)) return true;
			}

			return false;
		}

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