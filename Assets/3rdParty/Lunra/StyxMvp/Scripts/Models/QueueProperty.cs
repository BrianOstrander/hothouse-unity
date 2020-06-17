using System;
using System.Collections.Generic;
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
			public readonly Events Event;
			public readonly object Source;
			public readonly T Element;

			public Delta(
				Events events,
				object source = default,
				T element = default
			)
			{
				Event = events;
				Source = source;
				Element = element;
			}
		}
		
		public readonly string Name;

		public event Action Changed = ActionExtensions.Empty;
		public event Action<Delta> ChangedDelta = ActionExtensions.GetEmpty<Delta>();
		
		readonly Queue<T> queue;

		public void Clear(
			object source = default
		)
		{
			if (queue.None()) return;
			queue.Clear();
			Changed();
			ChangedDelta(
				new Delta(
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
			queue.Enqueue(element);
			Changed();
			ChangedDelta(
				new Delta(
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
			var result = queue.Dequeue();
			Changed();
			ChangedDelta(
				new Delta(
					Events.Dequeue,
					source,
					result
				)
			);
			return result;
		}

		public T Peek() => queue.Peek();
		public T[] PeekAll() => queue.ToArray();

		public QueueProperty(
			Queue<T> queue,
			string name,
			params Action[] listeners
		)
		{
			Name = name;
			this.queue = queue;

			foreach (var listener in listeners) Changed += listener;
		}

		public QueueProperty(
			Queue<T> queue,
			params Action[] listeners
		) : this(
			queue,
			null,
			listeners
		) {}
	}
}