using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Lunra.Satchel
{
	public class ItemStore
	{
		public enum UpdateTypes
		{
			Unknown = 0,
			ItemNew = 10,
			ItemUpdated = 20
		}
		
		[JsonProperty] Dictionary<ulong, Item> items = new Dictionary<ulong, Item>();
		[JsonProperty] ulong currentId;
		[JsonProperty] DateTime lastUpdated;
		[JsonProperty] UpdateTypes lastUpdatedType;

		public event Action<DateTime, UpdateTypes, Item> Updated;
		
		public void Initialize()
		{
			
		}

		public Item New(
			Action<Item> initialize = null
		)
		{
			var itemId = currentId;
			currentId++;
			
			var result = new Item(itemId);
			
			initialize?.Invoke(result);
			
			items.Add(itemId, result);

			result.Initialize(this, OnUpdated);
			
			return result;
		}

		public bool TryGet(
			ulong id,
			out Item item
		)
		{
			return items.TryGetValue(id, out item);
		}

		public Item Get(ulong id) => items[id];

		void OnUpdated(DateTime time, UpdateTypes type, Item item)
		{
			lastUpdated = time;
			lastUpdatedType = type;
			Updated?.Invoke(time, type, item);
		}
	}
}