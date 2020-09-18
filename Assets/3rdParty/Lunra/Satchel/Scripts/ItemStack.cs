using System;

namespace Lunra.Satchel
{
	public struct ItemStack
	{
		public static ItemStack Of(Item item, Stack stack) => new ItemStack(item, stack);
		public static ItemStack Of(Item item, int count) => new ItemStack(item, item.StackOf(count));
		public static ItemStack OfZero(Item item) => Of(item, 0);
		public static ItemStack OfAll(Item item) => Of(item, item.InstanceCount);
		
		public Item Item { get; }
		public Stack Stack { get; }

		public long Id => Stack.Id;
		public int Count => Stack.Count;

		ItemStack(
			Item item,
			Stack stack
		)
		{
			Item = item ?? throw new ArgumentNullException(nameof(item));
			
			if (item.Id != stack.Id) throw new ArgumentException($"Provided item {item} does not match id of stack {stack.Id}");
			
			Stack = stack;
		}

		public override string ToString() => $"( {nameof(ItemStack)} [ {Item.Id} ] : {Count} )";
	}
}