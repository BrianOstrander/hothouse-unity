using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Satchel
{
	public struct ItemStack
	{
		[JsonProperty] public ulong Id { get; private set; }
		[JsonProperty] public int Count { get; private set; }

		public ItemStack(ulong id, int count)
		{
			Id = id;
			Count = count;
		}
		
		public ItemStack GetEmpty() => new ItemStack(Id, 0);
		public ItemStack GetCount(int count) => new ItemStack(Id, Mathf.Max(0, count));

		public bool Is(ulong id) => Id == id;
		public bool Is(ulong id, int count) => Id == id && Count == count;
		public bool Is(ItemStack itemStack) => Is(itemStack.Id, itemStack.Count);

		public static ItemStack operator +(ItemStack itemStack, int count) => itemStack.GetCount(itemStack.Count + count);
		public static ItemStack operator -(ItemStack itemStack, int count) => itemStack.GetCount(itemStack.Count - count);
		public static ItemStack operator *(ItemStack itemStack, int count) => itemStack.GetCount(itemStack.Count * count);
		public static ItemStack operator /(ItemStack itemStack, int count) => itemStack.GetCount(itemStack.Count / count);
		
		public static ItemStack operator ++(ItemStack itemStack) => itemStack.GetCount(itemStack.Count + 1);
		public static ItemStack operator --(ItemStack itemStack) => itemStack.GetCount(itemStack.Count - 1);
	}
}