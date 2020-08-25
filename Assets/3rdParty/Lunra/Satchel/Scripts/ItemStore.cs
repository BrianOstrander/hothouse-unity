using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Lunra.Core;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Satchel
{
	public class ItemStore
	{
		public struct Event
		{
			public enum Formats
			{
				Default = 0,
				IncludeItems = 1 << 0,
				IncludeProperties = 1 << 1
			}
			
			[JsonProperty] public DateTime UpdateTime { get; private set; }
			[JsonProperty] public Item.Event.Types[] Updates { get; private set; }
			[JsonProperty] public Item.Event[] ItemEvents { get; private set; }

			public Event(
				DateTime updateTime,
				Item.Event.Types[] updates,
				Item.Event[] itemEvents
			)
			{
				UpdateTime = updateTime;
				Updates = updates;
				ItemEvents = itemEvents;
			}

			public override string ToString() => ToString(Formats.Default);

			public string ToString(Formats format)
			{
				var result = $"Updated {ItemEvents.Length} Item(s) |";

				foreach (var updateType in Updates) result += " " + Item.Event.ReadableUpdateType(updateType);

				result += " | "+UpdateTime;

				if (format == Formats.Default) return result;

				foreach (var itemEvent in ItemEvents)
				{
					result += "\n" + itemEvent.ToString(format.HasFlag(Formats.IncludeProperties));
				}

				return result;
			}
		}
		
		[JsonProperty] Dictionary<ulong, Item> items = new Dictionary<ulong, Item>();
		[JsonProperty] ulong currentId;
		[JsonProperty] DateTime lastUpdated;
		
		string[] ignoredKeysForStacking;
		string[] ignoredKeysCloning;
		IItemModifier[] modifiers;
		
		[JsonIgnore] public ValidationStore Validation { get; private set; }
		
		public event Action<Event> Updated;

		public void Initialize(
			string[] ignoredKeysForStacking = null,
			string[] ignoredKeysCloning = null,
			IItemModifier[] modifiers = null
		)
		{
			this.ignoredKeysForStacking = new[]
				{
					Constants.InstanceCount.Key
				}
				.Concat(ignoredKeysForStacking ?? new string[0])
				.Distinct()
				.ToArray();
			
			this.ignoredKeysCloning = new[]
				{
					Constants.InstanceCount.Key
				}
				.Concat(ignoredKeysCloning ?? new string[0])
				.Distinct()
				.ToArray();
			
			this.modifiers = new []
				{
					new CallbackItemModifier(i => i.Set(Constants.InstanceCount, 0))
				}
				.Concat(modifiers ?? new IItemModifier[0])
				.ToArray();

			Validation = new ValidationStore().Initialize(this);
			
			foreach (var kv in items) kv.Value.Initialize(this, i => TryUpdate(i));
		}

		Item Define(Action<Item> initialize)
		{
			var itemId = currentId;
			currentId++;
			
			var item = new Item(itemId);

			initialize(item);
			
			foreach (var modifier in modifiers)
			{
				if (modifier.IsValid(item)) modifier.Apply(item);
			}
					
			items.Add(item.Id, item);

			item.Initialize(
				this,
				i => TryUpdate(i)
			);

			return item;
		}

		public Item New(params (string Key, object Value)[] propertyKeyValues) => Define(item => item.Set(propertyKeyValues));
		
		public Item New(
			Item source,
			params (string Key, object Value)[] propertyKeyValues
		)
		{
			return Define(
				item =>
				{
					item.CloneProperties(
						source,
						ignoredKeysCloning
					);
			
					item.Set(propertyKeyValues);
				}
			);
		}

		public bool CanStack(Item item0, Item item1)
		{
			if (item0.Id == item1.Id) return true;
			
			var targetKeys = item0.PropertyKeys;
			var sourceKeys = item1.PropertyKeys;

			if (targetKeys.Length != sourceKeys.Length) return false;
			if (targetKeys.Length == 0) return true;

			foreach (var targetKey in targetKeys)
			{
				if (!sourceKeys.Contains(targetKey)) return false;
				
				if (ignoredKeysForStacking.Contains(targetKey)) continue;
				
				if (!Item.IsPropertyEqual(item0, item1, targetKey)) return false;
			}

			return true;
		}

		public bool CanStack(ItemStack stack0, ItemStack stack1) => CanStack(First(stack0.Id), First(stack1.Id));

		public ItemStack NewStack(ulong id, int count)
		{
			if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "Negative counts are not supported for stacks");

			var item = First(id);
			var result = new ItemStack(id, count);

			if (count == 0) return result;

			item.Set(Constants.InstanceCount, count + item.Get(Constants.InstanceCount));

			return result;
		}

		public ItemStack NewStack(Item item, int count) => NewStack(item.Id, count);

		public void Destroy(ulong id, int count)
		{
			if (count == 0) return;
			if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "Negative counts are not supported for stacks");
			
			var item = First(id);
			var itemInstanceCount = item.Get(Constants.InstanceCount);
			var newInstanceCount = itemInstanceCount;

			if (itemInstanceCount < count)
			{
				Debug.LogError($"Instance count {itemInstanceCount} is less than stack count {count}, this is unexpected");
				newInstanceCount = 0;
			}
			else newInstanceCount -= count;

			if (itemInstanceCount == newInstanceCount) return;

			item.Set(Constants.InstanceCount, newInstanceCount);
		}

		public void Destroy(ItemStack stack) => Destroy(stack.Id, stack.Count);

		public bool TryGet(ulong id, out Item item) => items.TryGetValue(id, out item);

		public bool TryGet(Func<Item, bool> predicate, out Item item)
		{
			try
			{
				item = First(predicate);
				return true;
			}
			catch (InvalidOperationException)
			{
				item = default;
				return false;
			}
		}
		
		public Item First(ulong id) => items[id];
		public Item First(Func<Item, bool> predicate) => items.First(kv => predicate(kv.Value)).Value;
		
		public Item FirstOrFallback(ulong id, Item fallback = null) => items.TryGetValue(id, out var value) ? value : fallback;

		public Item FirstOrFallback(Func<Item, bool> predicate, Item fallback = null)
		{
			try
			{
				return First(predicate);
			}
			catch (InvalidOperationException)
			{
				return fallback;
			}
		}

		public Item FirstOrDefault(ulong id) => FirstOrFallback(id);
		public Item FirstOrDefault(Func<Item, bool> predicate) => FirstOrFallback(predicate);

		public Item[] Where(Func<Item, bool> predicate) => items.Select(kv => kv.Value).Where(predicate).ToArray();
		public Item[] ToArray() => items.Select(kv => kv.Value).ToArray();

		public bool Cleanup(out Item[] removed)
		{
			removed = items
				.Where(kv => !kv.Value.TryGet(Constants.IgnoreCleanup, out var isIgnored) || !isIgnored)
				.Select(kv => kv.Value)
				.ToArray();
		
			if (removed.None()) return false;

			var updateTime = DateTime.Now;
			var updates = new[] {Item.Event.Types.Item | Item.Event.Types.Destroyed}; 
			var emptyPropertyUpdates = new Dictionary<string, (Item.Property Property, Item.Event.Types Update)>().ToReadonlyDictionary();
			
			var itemEventsList = new List<Item.Event>();

			foreach (var item in removed)
			{
				items.Remove(item.Id);
				item.ForceUpdateTime(updateTime);
				itemEventsList.Add(
					new Item.Event(
						item.Id,
						updateTime,
						updates,
						emptyPropertyUpdates
					)	
				);
			}

			return TryUpdate(
				itemEventsList.ToArray()
			);
		}

		public bool Cleanup() => Cleanup(out _);
		
		#region Events
		bool TryUpdate(
			params Item.Event[] itemEvents
		)
		{
			if (itemEvents.None()) return false;

			var updateTime = lastUpdated;
			var eventTypeProperty = Item.Event.Types.Property;
			var eventTypeItem = Item.Event.Types.Item;

			var itemEventsList = new List<Item.Event>();
			
			foreach (var itemEvent in itemEvents)
			{
				var currentEventTypeProperty = Item.Event.Types.Property;
				var currentEventTypeItem = Item.Event.Types.Item;
				
				foreach (var itemEventUpdate in itemEvent.Updates)
				{
					if (itemEventUpdate.HasFlag(Item.Event.Types.Property)) currentEventTypeProperty |= itemEventUpdate;
					else if (itemEventUpdate.HasFlag(Item.Event.Types.Item)) currentEventTypeItem |= itemEventUpdate;
				}
				
				if (currentEventTypeProperty == Item.Event.Types.Property && currentEventTypeItem == Item.Event.Types.Item) continue;
				
				if (updateTime < itemEvent.UpdateTime) updateTime = itemEvent.UpdateTime;
				eventTypeProperty |= currentEventTypeProperty;
				eventTypeItem |= currentEventTypeItem;
				
				itemEventsList.Add(itemEvent);
			}
			
			var eventTypeList = new List<Item.Event.Types>();
			
			if (eventTypeProperty != Item.Event.Types.Property) eventTypeList.Add(eventTypeProperty);
			if (eventTypeItem != Item.Event.Types.Item) eventTypeList.Add(eventTypeItem);

			if (eventTypeList.None()) return false;
			
			lastUpdated = updateTime;
			
			Updated?.Invoke(
				new Event(
					updateTime,
					eventTypeList.ToArray(),
					itemEventsList.ToArray()
				)
			);

			return true;
		}
		#endregion

		public override string ToString() => ToString(false, false);
		
		public string ToString(
			bool verbose,
			bool includeProperties
		)
		{
			var result = $"ItemStore Contains {items.Count} Item(s) | Last Updated {lastUpdated}";
			if (!verbose) return result;

			foreach (var item in items)
			{
				result += $"\n\t{item.Value.ToString(includeProperties ? Item.Formats.IncludeProperties | Item.Formats.ExtraPropertyIndent : Item.Formats.Default)}";
			}

			return result;
		}
	}
}