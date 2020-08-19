using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Lunra.Satchel
{
	public class ItemStore
	{
		[JsonProperty] Dictionary<ulong, Item> items = new Dictionary<ulong, Item>();
		[JsonProperty] ulong currentId;
		[JsonProperty] DateTime lastUpdated;

		public event Action<Item.Event> Updated;

		IItemModifier[] modifiers;
		
		public void Initialize(
			params IItemModifier[] modifiers
		)
		{
			this.modifiers = new []
				{
					new CallbackItemModifier(i => i.Set(Constants.InstanceCount, 0))
				}
				.Concat(modifiers)
				.ToArray();
			
			foreach (var kv in items) kv.Value.Initialize(OnUpdated);
		}

		(Item Item, Action Done) Create()
		{
			var itemId = currentId;
			currentId++;
			
			var item = new Item(itemId);
			
			return (
				item,
				() =>
				{
					foreach (var modifier in modifiers)
					{
						if (modifier.IsValid(item)) modifier.Apply(item);
					}
					
					items.Add(item.Id, item);

					item.Initialize(OnUpdated);
				}
			);
		}
		
		public Item New()
		{
			var result = Create();

			result.Done();

			return result.Item;
		}
		
		public Item New(
			Action<Item> initialize
		)
		{
			var result = Create();

			initialize(result.Item);
			
			result.Done();

			return result.Item;
		}
		
		public Item New(params (string Key, object Value)[] propertyKeyValues)
		{
			var result = Create();

			result.Item.Set(propertyKeyValues);
			
			result.Done();

			return result.Item;
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

		#region Events
		void OnUpdated(Item.Event update)
		{
			lastUpdated = update.UpdateTime;
			Updated?.Invoke(update);
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
				result += $"\n\t{item.Value.ToString(includeProperties ? Item.Formats.ExcludePrefix | Item.Formats.IncludeProperties | Item.Formats.ExtraPropertyIndent : Item.Formats.ExcludePrefix)}";
			}

			return result;
		}
	}
}