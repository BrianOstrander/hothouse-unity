using System;
using System.Collections.Generic;
using Lunra.Core;

namespace Lunra.StyxMvp.Models
{
	public class DictionaryProperty<TKey, TValue>
	{
		public enum Events
		{
			Unknown = 0,
			Add = 10,
			Update = 20,
			Remove = 40,
			Clear = 30
		}

		public struct Delta
		{
			public readonly DictionaryProperty<TKey, TValue> Property;
			public readonly Events Event;
			public readonly object Source;
			public readonly TKey Key;
			public readonly TValue Value;

			public Delta(
				DictionaryProperty<TKey, TValue> property,
				Events events,
				object source = default,
				TKey key = default,
				TValue value = default
			)
			{
				Property = property;
				Event = events;
				Source = source;
				Key = key;
				Value = value;
			}
		}
		
		public readonly string Name;

		public event Action<DictionaryProperty<TKey, TValue>> Changed = ActionExtensions.GetEmpty<DictionaryProperty<TKey, TValue>>();
		public event Action<Delta> ChangedDelta = ActionExtensions.GetEmpty<Delta>();
		
		readonly Dictionary<TKey, TValue> dictionary;

		public void Clear(
			object source = default
		)
		{
			if (dictionary.None()) return;
			dictionary.Clear();
			Changed(this);
			ChangedDelta(
				new Delta(
					this,
					Events.Clear,
					source
				)
			);
		}

		public void Add(
			TKey key,
			TValue value,
			object source = default
		)
		{
			dictionary.Add(key, value);
			Changed(this);
			ChangedDelta(
				new Delta(
					this,
					Events.Add,
					source,
					key,
					value
				)
			);
		}

		public bool TryGetValue(
			TKey key,
			out TValue value
		)
		{
			return dictionary.TryGetValue(key, out value);
		}
		
		public bool Remove(
			TKey key,
			object source = default
		)
		{
			var result = dictionary.Remove(key);
			if (!result) return false;
			
			Changed(this);
			ChangedDelta(
				new Delta(
					this,
					Events.Remove,
					source,
					key
				)
			);
			
			return true;
		}

		public TValue this[TKey key]
		{
			get => dictionary[key];

			set => Update(key, value);
		}

		public void Update(
			TKey key,
			TValue value,
			object source = null
		)
		{
			if (!TryGetValue(key, out var currentValue))
			{
				Add(key, value);
				return;
			}

			if (EqualityComparer<TValue>.Default.Equals(currentValue, value)) return;
			
			dictionary[key] = value;
			Changed(this);
			ChangedDelta(
				new Delta(
					this,
					Events.Update,
					source,
					key,
					value
				)
			);
		}

		public DictionaryProperty(
			Dictionary<TKey, TValue> dictionary,
			string name,
			params Action<DictionaryProperty<TKey, TValue>>[] listeners
		)
		{
			Name = name;
			this.dictionary = dictionary;

			foreach (var listener in listeners) Changed += listener;
		}

		public DictionaryProperty(
			Dictionary<TKey, TValue> dictionary,
			params Action<DictionaryProperty<TKey, TValue>>[] listeners
		) : this(
			dictionary,
			null,
			listeners
		) {}
	}
}