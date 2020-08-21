using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Satchel
{
	public struct ItemStack
	{
		/// <summary>
		/// The Id of the Item this stack contains.
		/// </summary>
		[JsonProperty] public ulong Id { get; private set; }
		[JsonProperty] public int Count { get; private set; }

		[JsonIgnore] public bool IsEmpty => Count == 0;

		public ItemStack(
			ulong id,
			int count
		)
		{
			Id = id;
			Count = Mathf.Max(0, count);
		}
		
		public ItemStack NewCount(int count) => new ItemStack(Id, count);

		public bool Is(ulong id) => id == Id;
		public bool Is(Item item) => item.Id == Id;
		public bool IsEqualTo(Item item, int count) => item.Id == Id && count == Count;
		public bool IsEqualTo(ulong id, int count) => id == Id && count == Count;
		public bool IsEqualTo(ItemStack itemStack) => IsEqualTo(itemStack.Id, itemStack.Count);

		public static ItemStack operator +(ItemStack itemStack, int count) => itemStack.NewCount(itemStack.Count + count);
		public static ItemStack operator -(ItemStack itemStack, int count) => itemStack.NewCount(itemStack.Count - count);
		public static ItemStack operator *(ItemStack itemStack, int count) => itemStack.NewCount(itemStack.Count * count);
		public static ItemStack operator /(ItemStack itemStack, int count) => itemStack.NewCount(itemStack.Count / count);
		
		public static ItemStack operator ++(ItemStack itemStack) => itemStack.NewCount(itemStack.Count + 1);
		public static ItemStack operator --(ItemStack itemStack) => itemStack.NewCount(itemStack.Count - 1);
	}
}