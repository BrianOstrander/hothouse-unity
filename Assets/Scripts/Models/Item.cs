using System;
using System.Linq;
using Lunra.Core;

namespace Lunra.Hothouse.Models
{
	public struct Item
	{
		public static Types[] Valid = EnumExtensions.GetValues(Types.Unknown);
		
		public static Item[] Empty { get; } = Populate(t => 0);
		
		public static Item[] Populate(Func<Types, int> predicate)
		{
			return EnumExtensions.GetValues(Types.Unknown).Select(t => new Item(predicate(t), t)).ToArray();
		}
		
		public static Item New(int count, Types type) => new Item(count, type);
		
		public static Item None() => new Item(0, Types.Unknown);
		
		public enum Types
		{
			Unknown = 0,
			Stalks = 10,
			Rations = 20,
			Scrap = 30,
		}

		public readonly int Count;
		public readonly Types Type;

		Item(
			int count,
			Types type
		)
		{
			Count = count;
			Type = type;
		}
		
		public Item NewCount(int count) => New(count, Type);
		public Item NewType(Types type) => New(Count, type);
	}
}