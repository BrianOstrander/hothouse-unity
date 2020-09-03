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
		
		#region Serialized
		[JsonProperty] Dictionary<long, Item> items = new Dictionary<long, Item>();
		[JsonProperty] DateTime lastUpdated;

		#endregion
		
		#region Non Serialized
		string[] ignoredKeysForStacking;
		string[] ignoredKeysCloning;
		IItemModifier[] modifiers;
		Dictionary<long, Inventory> inventories;
		Dictionary<long, Action<Event>> inventoryCallbacks;
		
		[JsonIgnore] public IdCounter IdCounter { get; private set; }
		[JsonIgnore] public BuilderUtility Builder { get; private set; }
		[JsonIgnore] public ValidationStore Validation { get; private set; }
		[JsonIgnore] public ProcessorStore Processor { get; private set; }
		[JsonIgnore] public ReadOnlyDictionary<long, Item> Items { get; private set; }
		[JsonIgnore] public ReadOnlyDictionary<long, Inventory> Inventories { get; private set; }
		#endregion
		
		public event Action<Event> Updated;

		public ItemStore Initialize(
			IdCounter idCounter,
			string[] ignoredKeysForStacking = null,
			string[] ignoredKeysCloning = null,
			IItemModifier[] modifiers = null
		)
		{
			IdCounter = idCounter ?? throw new ArgumentNullException(nameof(idCounter));
			
			this.ignoredKeysForStacking = (ignoredKeysForStacking ?? new string[0])
				.Distinct()
				.ToArray();
			
			this.ignoredKeysCloning = (ignoredKeysCloning ?? new string[0])
				.Distinct()
				.ToArray();

			this.modifiers = modifiers ?? new IItemModifier[0];

			Items = new ReadOnlyDictionary<long, Item>(items);
			Inventories = new ReadOnlyDictionary<long, Inventory>(inventories = new Dictionary<long, Inventory>());
			inventoryCallbacks = new Dictionary<long, Action<Event>>();
			
			Builder = new BuilderUtility(this);
			Validation = new ValidationStore().Initialize(this);
			Processor = new ProcessorStore().Initialize(this);
			
			foreach (var kv in items) kv.Value.Initialize(this, i => TryUpdate(i));

			return this;
		}

		public void Register(
			Inventory inventory,
			Action<Event> update
		)
		{
			if (inventory.Id == IdCounter.UndefinedId) throw new Exception("Cannot register an inventory with an undefined Id");
			
			inventories.Add(inventory.Id, inventory);
			inventoryCallbacks.Add(inventory.Id, update);
		}
		
		public void UnRegister(Inventory inventory)
		{
			if (inventory.Id == IdCounter.UndefinedId) throw new Exception("Cannot unregister an inventory with an undefined Id");
			inventories.Remove(inventory.Id);
			inventoryCallbacks.Remove(inventory.Id);
		}

		/// <summary>
		/// Defines a new item with a unique id and initializes it.
		/// </summary>
		/// <remarks>
		/// Ideally this should only be called by instances of the <c>Inventory</c> class when creating new stacks. 
		/// </remarks>
		/// <param name="initialize"></param>
		/// <returns></returns>
		public Item Define(Action<Item> initialize = null)
		{
			var item = new Item(IdCounter.Next());

			initialize?.Invoke(item);
			
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

		/// <summary>
		/// Defines a new item cloned from a reference with a unique id and initializes it.
		/// </summary>
		/// <remarks>
		/// Ideally this should only be called by instances of the <c>Inventory</c> class when creating new stacks. 
		/// </remarks>
		/// <param name="reference">The item to clone.</param>
		/// <param name="initialize"></param>
		/// <returns></returns>
		public Item Define(
			Item reference,
			Action<Item> initialize = null
		)
		{
			return Define(
				item =>
				{
					item.CloneProperties(
						reference,
						ignoredKeysCloning
					);
			
					initialize?.Invoke(item);
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
				
				if (!Item.IsPropertyEqual(targetKey, item0, item1)) return false;
			}

			return true;
		}

		public bool CanStack(Stack stack0, Stack stack1) => CanStack(First(stack0.Id), First(stack1.Id));
		
		public bool TryGet(long id, out Item item) => items.TryGetValue(id, out item);

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
		
		public Item First(Stack stack) => items[stack.Id];
		public Item First(long id) => items[id];
		public Item First(Func<Item, bool> predicate) => items.First(kv => predicate(kv.Value)).Value;
		
		public Item FirstOrFallback(Stack stack, Item fallback = null) => items.TryGetValue(stack.Id, out var value) ? value : fallback;
		public Item FirstOrFallback(long id, Item fallback = null) => items.TryGetValue(id, out var value) ? value : fallback;

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

		public Item FirstOrDefault(Stack stack) => FirstOrFallback(stack.Id);
		public Item FirstOrDefault(long id) => FirstOrFallback(id);
		public Item FirstOrDefault(Func<Item, bool> predicate) => FirstOrFallback(predicate);
		public Item[] Where(Func<Item, bool> predicate) => items.Select(kv => kv.Value).Where(predicate).ToArray();
		public Item[] ToArray() => items.Select(kv => kv.Value).ToArray();

		/// <summary>
		/// For iterating over all items when you don't need to access the actual list.
		/// </summary>
		/// <remarks>
		/// Adding or removing items while iterating here will cause an exception.
		/// </remarks>
		/// <param name="iterator"></param>
		public void IterateAll(Action<Item> iterator)
		{
			foreach (var kv in items)
			{
				try { iterator(kv.Value); }
				catch (Exception e) { Debug.LogException(e); }
			}
		}
		
		/// <summary>
		/// Iterates over the items in the provided stacks.
		/// </summary>
		/// <param name="iterator"></param>
		/// <param name="stacks"></param>
		public void Iterate(
			Action<Item, Stack> iterator,
			params Stack[] stacks
		)
		{
			foreach (var stack in stacks)
			{
				if (TryGet(stack.Id, out var item))
				{
					try { iterator(item, stack); }
					catch (Exception e) { Debug.LogException(e); }
				}
				else Debug.LogError($"Unable to find item with Id {stack.Id}");
			}
		}

		public void Cleanup()
		{
			var destructionEvents = new List<Item.Event>();
			var updateTime = DateTime.Now;
			
			foreach (var item in items.Values)
			{
				if (item.NoInstances)
				{
					if (item.InventoryId != IdCounter.UndefinedId) Debug.LogError($"Item with id [ {item.Id} ] has zero instances but still belongs to inventory [ {item.InventoryId} ], unexpected behaviour may occur");
				}
				else if (item.InventoryId != IdCounter.UndefinedId) continue;
				
				item.ForceUpdateTime(updateTime);
				
				destructionEvents.Add(
					new Item.Event(
						item.Id,
						item.InventoryId,
						updateTime,
						new [] { Item.Event.Types.Item | Item.Event.Types.Destroyed },
						new Dictionary<string, (Property Property, Item.Event.Types Update)>().ToReadonlyDictionary()
					)	
				);
			}

			foreach (var destructionEvent in destructionEvents) items.Remove(destructionEvent.Id);
			
			TryUpdate(destructionEvents.ToArray());
		}

		#region Events
		/// <summary>
		/// Triggers an update if changes occured.
		/// </summary>
		/// <param name="itemEvents"></param>
		/// <returns><c>true</c> if changes occured, <c>false</c> otherwise.</returns>
		bool TryUpdate(
			params Item.Event[] itemEvents
		)
		{
			if (itemEvents.None()) return false;

			var updateTime = lastUpdated;
			var eventTypeProperty = Item.Event.Types.Property;
			var eventTypeItem = Item.Event.Types.Item;

			var itemEventsByInventoryId = new Dictionary<long, (Item.Event.Types PropertyUpdates, Item.Event.Types ItemUpdates, List<Item.Event> ItemEvents)>();
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

				if (itemEvent.InventoryId == IdCounter.UndefinedId) continue;

				if (!itemEventsByInventoryId.TryGetValue(itemEvent.InventoryId, out var itemUpdate))
				{
					itemUpdate.PropertyUpdates = Item.Event.Types.Property;
					itemUpdate.ItemUpdates = Item.Event.Types.Item;
					itemUpdate.ItemEvents = new List<Item.Event>();
				}

				itemUpdate.PropertyUpdates |= currentEventTypeProperty;
				itemUpdate.ItemUpdates |= currentEventTypeItem;
				itemUpdate.ItemEvents.Add(itemEvent);

				itemEventsByInventoryId[itemEvent.InventoryId] = itemUpdate;
			}
			
			var eventTypeList = new List<Item.Event.Types>();
			
			if (eventTypeProperty != Item.Event.Types.Property) eventTypeList.Add(eventTypeProperty);
			if (eventTypeItem != Item.Event.Types.Item) eventTypeList.Add(eventTypeItem);

			if (eventTypeList.None()) return false;
			
			lastUpdated = updateTime;

			var eventResult = new Event(
				updateTime,
				eventTypeList.ToArray(),
				itemEventsList.ToArray()
			);
			
			Updated?.Invoke(eventResult);

			foreach (var itemUpdate in itemEventsByInventoryId)
			{
				if (!inventoryCallbacks.TryGetValue(itemUpdate.Key, out var callback)) continue;

				var updates = new List<Item.Event.Types>();
				
				if (itemUpdate.Value.ItemUpdates != Item.Event.Types.Item) updates.Add(itemUpdate.Value.ItemUpdates);
				if (itemUpdate.Value.PropertyUpdates != Item.Event.Types.Property) updates.Add(itemUpdate.Value.PropertyUpdates);
				
				callback(
					new Event(
						updateTime,
						updates.ToArray(),
						itemUpdate.Value.ItemEvents.ToArray()
					)	
				);
			}
			
			return true;
		}
		#endregion

		public override string ToString() => ToString(true, true);
		
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