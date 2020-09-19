using System;
using System.Collections.Generic;
using System.Linq;

namespace Lunra.Satchel
{
	public class ItemBuilder
	{
		ItemStore itemStore;
		Dictionary<string, PropertyKeyValue> properties = new Dictionary<string, PropertyKeyValue>();

		public ItemBuilder(
			ItemStore itemStore
		)
		{
			this.itemStore = itemStore ?? throw new ArgumentNullException(nameof(itemStore));
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

		public Stack Done() => Done(1);
		
		public Stack Done(int count) => Done(count, out _);

		public Stack Done(out Item item) => Done(1, out item);
		
		public Stack Done(
			int count,
			out Item item
		)
		{
			if (count < 1) throw new ArgumentOutOfRangeException(nameof(count), "Must be greater than zero");

			var propertyKeyValues = properties.Values.ToArray();
			
			item = itemStore.Define(
				i =>
				{
					i.Set(propertyKeyValues);
					i.ForceUpdateInstanceCount(count);
				}
			);
			return item.StackOf(count);
		}
		
		public static implicit operator Stack(ItemBuilder builder) => builder.Done();
	}
}