using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Satchel
{
	public struct ItemStack
	{
		[JsonProperty] public ulong ItemId { get; private set; }
		[JsonProperty] public int Count { get; private set; }

		public ItemStack(
			ulong itemId,
			int count
		)
		{
			ItemId = itemId;
			Count = count;
		}
		
		public ItemStack NewCount(int count) => new ItemStack(ItemId, Mathf.Max(0, count));

		public bool Is(ulong itemId) => itemId == ItemId;
		public bool Is(ulong itemId, int count) => itemId == ItemId && count == Count;
		public bool Is(ItemStack itemStack) => Is(itemStack.ItemId, itemStack.Count);

		public static ItemStack operator +(ItemStack itemStack, int count) => itemStack.NewCount(itemStack.Count + count);
		public static ItemStack operator -(ItemStack itemStack, int count) => itemStack.NewCount(itemStack.Count - count);
		public static ItemStack operator *(ItemStack itemStack, int count) => itemStack.NewCount(itemStack.Count * count);
		public static ItemStack operator /(ItemStack itemStack, int count) => itemStack.NewCount(itemStack.Count / count);
		
		public static ItemStack operator ++(ItemStack itemStack) => itemStack.NewCount(itemStack.Count + 1);
		public static ItemStack operator --(ItemStack itemStack) => itemStack.NewCount(itemStack.Count - 1);
	}
}