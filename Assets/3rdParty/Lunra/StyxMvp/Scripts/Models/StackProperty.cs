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
		
		readonly Stack<T> stack;

		public void Clear(
			object source = default
		)
		{
			if (stack.None()) return;
			stack.Clear();
			Changed();
			ChangedDelta(
				new Delta(
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
			stack.Push(element);
			Changed();
			ChangedDelta(
				new Delta(
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
			var result = stack.Pop();
			Changed();
			ChangedDelta(
				new Delta(
					Events.Pop,
					source,
					result
				)
			);
			return result;
		}

		public bool TryPeek(out T element)
		{
			element = default;
			
			if (stack.Any())
			{
				element = stack.Peek();
				return true;
			}

			return false;
		}
		
		public T Peek() => stack.Peek();
		public T[] PeekAll() => stack.ToArray();

		public StackProperty(
			Stack<T> stack,
			string name,
			params Action[] listeners
		)
		{
			Name = name;
			this.stack = stack;

			foreach (var listener in listeners) Changed += listener;
		}

		public StackProperty(
			Stack<T> stack,
			params Action[] listeners
		) : this(
			stack,
			null,
			listeners
		) {}
	}
}