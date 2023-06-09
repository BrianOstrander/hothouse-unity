using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;

namespace Lunra.StyxMvp.Models
{
	public class QueueProperty<T>
	{
		public enum Events
		{
			Unknown = 0,
			Enqueue = 10,
			Dequeue = 20,
			Clear = 30
		}

		public struct Delta
		{
			public readonly QueueProperty<T> Property;
			public readonly Events Event;
			public readonly object Source;
			public readonly T Element;

			public Delta(
				QueueProperty<T> property,
				Events events,
				object source = default,
				T element = default
			)
			{
				Property = property;
				Event = events;
				Source = source;
				Element = element;
			}
		}
		
		public readonly string Name;

		public event Action<QueueProperty<T>> Changed = ActionExtensions.GetEmpty<QueueProperty<T>>();
		public event Action<Delta> ChangedDelta = ActionExtensions.GetEmpty<Delta>();
		
		// Since getting burned by the way Newtonsoft messes up Stacks, I've decided to not trust Queues either.
		readonly List<T> queue;

		public void Clear(
			object source = default
		)
		{
			if (queue.None()) return;
			queue.Clear();
			Changed(this);
			ChangedDelta(
				new Delta(
					this,
					Events.Clear,
					source
				)
			);
		}

		public void Enqueue(
			T element,
			object source = default
		)
		{
			queue.Add(element);
			
			Changed(this);
			ChangedDelta(
				new Delta(
					this,
					Events.Enqueue,
					source,
					element
				)
			);
		}

		public bool TryDequeue(
			out T element,
			object source = default
		)
		{
			element = default;
			if (queue.None()) return false;
			
			element = Dequeue(source);
			return true;
		}
		
		public T Dequeue(
			object source = default
		)
		{
			var result = queue[0];
			queue.RemoveAt(0);
			
			Changed(this);
			ChangedDelta(
				new Delta(
					this,
					Events.Dequeue,
					source,
					result
				)
			);
			return result;
		}

		public bool TryPeek(out T element) => TryPeek(out element, 0); 
		
		public bool TryPeek(out T element, int offset)
		{
			if (offset < queue.Count)
			{
				element = queue[offset];
				return true;
			}
			
			element = default;
			return false;
		}
		
		public T Peek() => Peek(0);
		public T Peek(int offset) => queue[offset];
		public T[] PeekAll() => queue.ToArray();

		public QueueProperty(
			List<T> queue,
			string name,
			params Action<QueueProperty<T>>[] listeners
		)
		{
			Name = name;
			this.queue = queue;

			foreach (var listener in listeners) Changed += listener;
		}

		public QueueProperty(
			List<T> queue,
			params Action<QueueProperty<T>>[] listeners
		) : this(
			queue,
			null,
			listeners
		) {}
	}
}