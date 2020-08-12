using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;

namespace Lunra.StyxMvp.Models
{
	public class StackProperty<T>
	{
		public enum Events
		{
			Unknown = 0,
			Push = 10,
			Pop = 20,
			Clear = 30
		}

		public struct Delta
		{
			public readonly StackProperty<T> Property;
			public readonly Events Event;
			public readonly object Source;
			public readonly T Element;

			public Delta(
				StackProperty<T> property,
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

		public event Action<StackProperty<T>> Changed = ActionExtensions.GetEmpty<StackProperty<T>>();
		public event Action<Delta> ChangedDelta = ActionExtensions.GetEmpty<Delta>();
		
		// For some boring reason I don't wanna read about Newtonsoft doesn't like Stacks, so we're using a list instead.
		readonly List<T> stack;

		public void Clear(
			object source = default
		)
		{
			if (stack.None()) return;
			stack.Clear();
			Changed(this);
			ChangedDelta(
				new Delta(
					this,
					Events.Clear,
					source
				)
			);
		}

		public void Push(
			T element,
			object source = default
		)
		{
			stack.Insert(0, element);
			Changed(this);
			ChangedDelta(
				new Delta(
					this,
					Events.Push,
					source,
					element
				)
			);
		}

		public bool TryPop(
			out T element,
			object source = default
		)
		{
			element = default;
			if (stack.None()) return false;
			
			element = Pop(source);
			return true;
		}
		
		public T Pop(
			object source = default
		)
		{
			var result = stack[0];
			stack.RemoveAt(0);
			
			Changed(this);
			ChangedDelta(
				new Delta(
					this,
					Events.Pop,
					source,
					result
				)
			);
			return result;
		}

		public bool TryPeek(out T element) => TryPeek(out element, 0);

		public bool TryPeek(out T element, int offset)
		{
			if (offset < stack.Count)
			{
				element = stack[offset];
				return true;
			}

			element = default;
			return false;	
		}
		
		public T Peek() => Peek(0);
		public T Peek(int offset) => stack[offset];
		public T[] PeekAll() => stack.ToArray();

		public StackProperty(
			List<T> stack,
			string name,
			params Action<StackProperty<T>>[] listeners
		)
		{
			Name = name;
			this.stack = stack;

			foreach (var listener in listeners) Changed += listener;
		}

		public StackProperty(
			List<T> stack,
			params Action<StackProperty<T>>[] listeners
		) : this(
			stack,
			null,
			listeners
		) {}
	}
}