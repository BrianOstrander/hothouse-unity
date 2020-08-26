using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Satchel
{
	public struct Stack : IEquatable<Stack>
	{
		/// <summary>
		/// The Id of the Item this stack contains.
		/// </summary>
		[JsonProperty] public ulong Id { get; private set; }
		[JsonProperty] public int Count { get; private set; }

		[JsonIgnore] public bool IsEmpty => Count == 0;
		[JsonIgnore] public bool IsNotEmpty => !IsEmpty;

		public Stack(
			ulong id,
			int count
		)
		{
			if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "Cannot be less than zero");
			
			Id = id;
			Count = Mathf.Max(0, count);
		}
		
		public Stack NewCount(int count) => new Stack(Id, count);
		public Stack NewEmpty() => new Stack(Id, 0);

		public bool Is(ulong id) => id == Id;
		public bool Is(Item item) => item.Id == Id;

		public bool IsNot(ulong id) => !Is(id);
		public bool IsNot(Item item) => !Is(item);

		public bool Equals(Stack other) => Id == other.Id && Count == other.Count;

		public override bool Equals(object obj) => obj is Stack other && Equals(other);

		// This code was autogenerated by Rider...
		public override int GetHashCode()
		{
			unchecked
			{
				// ReSharper disable NonReadonlyMemberInGetHashCode
				return (Id.GetHashCode() * 397) ^ Count;
				// ReSharper restore NonReadonlyMemberInGetHashCode
			}
		}

		public static bool operator ==(Stack left, Stack right) => left.Equals(right);

		public static bool operator !=(Stack left, Stack right) => !left.Equals(right);
		
		public static Stack operator +(Stack stack, int count) => stack.NewCount(stack.Count + count);
		public static Stack operator -(Stack stack, int count) => stack.NewCount(stack.Count - count);
		public static Stack operator *(Stack stack, int count) => stack.NewCount(stack.Count * count);
		public static Stack operator /(Stack stack, int count) => stack.NewCount(stack.Count / count);
		
		public static Stack operator ++(Stack stack) => stack.NewCount(stack.Count + 1);
		public static Stack operator --(Stack stack) => stack.NewCount(stack.Count - 1);

		public static implicit operator Stack((Item Item, int Count) source) => new Stack(source.Item?.Id ?? Item.UndefinedId, source.Count);

		public override string ToString() => $"[ {Id} ] : {Count}";

		public string ToString(Item item, Item.Formats format = Item.Formats.Default)
		{
			if (item == null) return ToString() + " | < Null Item >";

			return item.ToString(format, Count);
		}
		public string ToString(ItemStore itemStore, Item.Formats format = Item.Formats.Default) => ToString(itemStore?.FirstOrDefault(Id), format);
	}
}