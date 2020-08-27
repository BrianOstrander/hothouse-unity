using System;
using System.Collections.Generic;
using System.Linq;

namespace Lunra.Satchel
{
	public class ItemBuilder
	{
		ItemStore itemStore;
		Inventory inventory;
		Dictionary<string, PropertyKeyValue> properties = new Dictionary<string, PropertyKeyValue>();

		public ItemBuilder(
			ItemStore itemStore,
			Inventory inventory
		)
		{
			this.itemStore = itemStore ?? throw new ArgumentNullException(nameof(itemStore));
			this.inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
		}

		public ItemBuilder WithProperties(
			PropertyKeyValue[] baseProperties,
			params PropertyKeyValue[] properties
		)
		{
			return WithProperties(baseProperties.Concat(properties).ToArray());
		}
		
		public ItemBuilder WithProperties(
			params PropertyKeyValue[] properties
		)
		{
			// This is to override duplicates
			foreach (var property in properties)
			{
				this.properties[property.Key] = property;
			}
			
			return this;
		}

		public bool Done(int count)
		{
			if (count < 1) throw new ArgumentOutOfRangeException(nameof(count), "Cannot be greater than zero");
			return Done(count, out _);
		}
		
		public bool Done(
			int count,
			out (Item Item, int Count) additions
		)
		{
			if (count < 1) throw new ArgumentOutOfRangeException(nameof(count), "Cannot be greater than zero");
			return inventory.New(
				count,
				out additions,
				properties.Values.ToArray()
			);
		}
	}
}