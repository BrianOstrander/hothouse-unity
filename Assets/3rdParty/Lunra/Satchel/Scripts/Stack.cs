using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Satchel
{
	public struct Stack
	{
		/// <summary>
		/// The Id of the Item this stack contains.
		/// </summary>
		[JsonProperty] public ulong Id { get; private set; }
		[JsonProperty] public int Count { get; private set; }

		[JsonIgnore] public bool IsEmpty => Count == 0;

		public Stack(
			ulong id,
			int count
		)
		{
			Id = id;
			Count = Mathf.Max(0, count);
		}
		
		public Stack NewCount(int count) => new Stack(Id, count);
		public Stack NewEmpty() => new Stack(Id, 0);

		public bool Is(ulong id) => id == Id;
		public bool Is(Item item) => item.Id == Id;
		public bool IsEqualTo(Item item, int count) => item.Id == Id && count == Count;
		public bool IsEqualTo(ulong id, int count) => id == Id && count == Count;
		public bool IsEqualTo(Stack stack) => IsEqualTo(stack.Id, stack.Count);

		public static Stack operator +(Stack stack, int count) => stack.NewCount(stack.Count + count);
		public static Stack operator -(Stack stack, int count) => stack.NewCount(stack.Count - count);
		public static Stack operator *(Stack stack, int count) => stack.NewCount(stack.Count * count);
		public static Stack operator /(Stack stack, int count) => stack.NewCount(stack.Count / count);
		
		public static Stack operator ++(Stack stack) => stack.NewCount(stack.Count + 1);
		public static Stack operator --(Stack stack) => stack.NewCount(stack.Count - 1);

		public override string ToString() => $"[ {Id} ] : {Count}";

		public string ToString(Item item, Item.Formats format = Item.Formats.Default)
		{
			if (item == null) return ToString() + " | < Null Item >";

			return item.ToString(format, Count);
		}
		public string ToString(ItemStore itemStore, Item.Formats format = Item.Formats.Default) => ToString(itemStore?.FirstOrDefault(Id), format);
	}
}