using System;
using System.Collections.Generic;
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
		[JsonProperty] Dictionary<ulong, Item> items = new Dictionary<ulong, Item>();
		[JsonProperty] ulong currentId = Item.UndefinedId + 1uL;
		[JsonProperty] DateTime lastUpdated;
		#endregion
		
		#region Non Serialized
		string[] ignoredKeysForStacking;
		string[] ignoredKeysCloning;
		IItemModifier[] modifiers;
		
		[JsonIgnore] public BuilderUtility Builder { get; private set; }
		[JsonIgnore] public ValidationStore Validation { get; private set; }
		[JsonIgnore] public ProcessorStore Processor { get; private set; }
		#endregion
		
		public event Action<Event> Updated;

		public ItemStore Initialize(
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
					Constants.InstanceCount.Key,
					Constants.Destroyed.Key
				}
				.Concat(ignoredKeysCloning ?? new string[0])
				.Distinct()
				.ToArray();
			
			this.modifiers = new []
				{
					new CallbackItemModifier(
						i => i.Set(Constants.InstanceCount, 0),
						i => !i.IsDefined(Constants.InstanceCount)
					),
					new CallbackItemModifier(
						i => i.Set(Constants.Destroyed, false)
					)
				}
				.Concat(modifiers ?? new IItemModifier[0])
				.ToArray();

			Builder = new BuilderUtility(this);
			Validation = new ValidationStore().Initialize(this);
			Processor = new ProcessorStore().Initialize(this);
			
			foreach (var kv in items) kv.Value.Initialize(this, i => TryUpdate(i));

			return this;
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
			var itemId = currentId;
			currentId++;
			
			var item = new Item(itemId);

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
				
				if (!Item.IsPropertyEqual(item0, item1, targetKey)) return false;
			}

			return true;
		}

		public bool CanStack(Stack stack0, Stack stack1) => CanStack(First(stack0.Id), First(stack1.Id));

		/// <summary>
		/// Destroys specified stacks from the item store, cleaning up any with an instance count of zero, unless the
		/// <c>IgnoreCleanup</c> flag is set to <c>true</c>.
		/// </summary>
		/// <param name="stacks"></param>
		/// <returns><c>true</c> if changes occured, <c>false</c> otherwise.</returns>
		public bool Destroy(params Stack[] stacks)
		{
			if (stacks.None()) return false;

			var consolidated = new Dictionary<ulong, (Item Item, int InstanceCount, int OldInstanceCount, Item.Event.Types ItemUpdates, Item.Event.Types PropertyUpdates, bool IgnoreCleanup)>();
			
			var totalItemUpdates = Item.Event.Types.Item;
			var totalPropertyUpdates = Item.Event.Types.Property;

			foreach (var stack in stacks)
			{
				if (stack.IsEmpty) continue;
				
				if (!consolidated.TryGetValue(stack.Id, out var entry))
				{
					if (items.TryGetValue(stack.Id, out var item))
					{
						var instanceCount = item.Get(Constants.InstanceCount);

						if (instanceCount < 0)
						{
							Debug.LogError($"Unexpected negative instance count of {instanceCount} for {item}");
							instanceCount = 0;
						}
						
						entry = (
							item,
							instanceCount,
							instanceCount,
							Item.Event.Types.Item,
							Item.Event.Types.Property,
							item.Get(Constants.IgnoreCleanup)
						);
					}
					else
					{
						Debug.LogError($"Unrecognized item id: {stack.Id}");
						entry = (
							null,
							0,
							0,
							Item.Event.Types.Item,
							Item.Event.Types.Property,
							false
						);
					}
				}

				entry.InstanceCount -= stack.Count;

				if (entry.Item != null && entry.InstanceCount != entry.OldInstanceCount)
				{
					entry.PropertyUpdates |= Item.Event.Types.Updated;
					totalPropertyUpdates |= Item.Event.Types.Updated;
					
					if (entry.InstanceCount <= 0 && !entry.IgnoreCleanup)
					{
						entry.ItemUpdates |= Item.Event.Types.Destroyed;
						totalItemUpdates |= Item.Event.Types.Destroyed;
					}
				}
				
				consolidated[stack.Id] = entry;
			}

			var totalUpdates = new List<Item.Event.Types>();
			
			if (totalItemUpdates != Item.Event.Types.Item) totalUpdates.Add(totalItemUpdates);
			if (totalPropertyUpdates != Item.Event.Types.Property) totalUpdates.Add(totalPropertyUpdates);

			// No stacks were destroyed or instance counts modified...
			if (totalUpdates.None()) return false;

			var updateTime = DateTime.Now;
			var itemEventsList = new List<Item.Event>();

			foreach (var entry in consolidated)
			{
				if (entry.Value.Item == null) continue;

				var instanceCount = Mathf.Max(0, entry.Value.InstanceCount);

				if (instanceCount == entry.Value.OldInstanceCount)
				{
					Debug.LogError($"Cannot destroy any instances of {entry.Value.Item}, there are none left to destroy.");
					continue;
				}

				var propertyEvents = new Dictionary<string, (Property Property, Item.Event.Types Update)>();
				Item.Event.Types[] updates;
				
				const Item.Event.Types ExpectedPropertyUpdate = Item.Event.Types.Property | Item.Event.Types.Updated;
				
				entry.Value.Item.Set(
					Constants.InstanceCount.Key,
					Mathf.Max(0, entry.Value.InstanceCount),
					out var instanceCountPropertyUpdate,
					true
				);
				
				if (instanceCountPropertyUpdate.Update != ExpectedPropertyUpdate)
				{
					Debug.LogError($"Expected {ExpectedPropertyUpdate:F} for {Constants.InstanceCount} but got {instanceCountPropertyUpdate.Update:F} instead");
				}

				if (instanceCount == 0)
				{
					updates = new [] {Item.Event.Types.Item | Item.Event.Types.Destroyed, Item.Event.Types.Property | Item.Event.Types.Updated}; 
					
					entry.Value.Item.Set(
						Constants.Destroyed.Key,
						true,
						out var destroyedPropertyUpdate,
						true
					);
					
					if (destroyedPropertyUpdate.Update != ExpectedPropertyUpdate)
					{
						Debug.LogError($"Expected {ExpectedPropertyUpdate:F} for {Constants.Destroyed} but got {destroyedPropertyUpdate.Update:F} instead");
					}
					
					items.Remove(entry.Value.Item.Id);
					
					propertyEvents.Add(
						Constants.Destroyed.Key,
						destroyedPropertyUpdate
					);
				}
				else
				{
					updates = new [] {Item.Event.Types.Property | Item.Event.Types.Updated};
				}
				
				propertyEvents.Add(
					Constants.InstanceCount.Key,
					instanceCountPropertyUpdate
				);

				entry.Value.Item.ForceUpdateTime(updateTime);
				
				itemEventsList.Add(
					new Item.Event(
						entry.Value.Item.Id,
						updateTime,
						updates,
						propertyEvents.ToReadonlyDictionary()
					)
				);
			}

			return TryUpdate(
				itemEventsList.ToArray()
			);
		}
		
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

		/// <summary>
		/// For iterating over all items when you don't need to access the actual list.
		/// </summary>
		/// <param name="iterator"></param>
		public void Iterate(Action<Item> iterator)
		{
			foreach (var kv in items)
			{
				try { iterator(kv.Value); }
				catch (Exception e) { Debug.LogException(e); }
			}
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