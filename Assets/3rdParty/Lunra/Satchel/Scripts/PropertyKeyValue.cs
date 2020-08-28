using UnityEngine;

namespace Lunra.Satchel
{
	public struct PropertyKeyValue
	{
		public string Key { get; }
		public Property Property { get; }

		public PropertyKeyValue(
			string key,
			Property property
		)
		{
			Key = key;
			Property = property;
		}
		
		public void Apply(
			Item item
		)
		{
			switch (Property.Type)
			{
				case Property.Types.Bool:
					item.Set(Key, Property.BoolValue);
					break;
				case Property.Types.Int:
					item.Set(Key, Property.IntValue);
					break;
				case Property.Types.Long:
					item.Set(Key, Property.LongValue);
					break;
				case Property.Types.Float:
					item.Set(Key, Property.FloatValue);
					break;
				case Property.Types.String:
					item.Set(Key, Property.StringValue);
					break;
				default: 
					Debug.LogError("Unrecognized Type: "+Property.Type);
					break;
			}
		}

		public void Apply(
			Item item,
			out (Property Property, Item.Event.Types Update) result,
			bool suppressUpdates
		)
		{
			switch (Property.Type)
			{
				case Property.Types.Bool:
					item.Set(
						Key,
						Property.BoolValue,
						out result,
						suppressUpdates
					);
					break;
				case Property.Types.Int:
					item.Set(
						Key,
						Property.IntValue,
						out result,
						suppressUpdates
					);
					break;
				case Property.Types.Long:
					item.Set(
						Key,
						Property.LongValue,
						out result,
						suppressUpdates
					);
					break;
				case Property.Types.Float:
					item.Set(
						Key,
						Property.FloatValue,
						out result,
						suppressUpdates
					);
					break;
				case Property.Types.String:
					item.Set(
						Key,
						Property.StringValue,
						out result,
						suppressUpdates
					);
					break;
				default: 
					Debug.LogError("Unrecognized Type: "+Property.Type);
					result = default;
					break;
			}
		}
	}
}