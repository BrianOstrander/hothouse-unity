using System.Collections.Generic;
using System.Linq;

namespace Lunra.Core
{
	public static class StackExtensions
	{
		public static bool TryPop<T>(
			this Stack<T> stack,
			out T element
		)
		{
			element = default;
			if (stack.None()) return false;
			
			element = stack.Pop();
			return true;
		}
		
		public static bool TryPeek<T>(
			this Stack<T> stack,
			out T element
		)
		{
			element = default;
			
			if (stack.Any())
			{
				element = stack.Peek();
				return true;
			}

			return false;
		}
		
		public static bool TryPeek<T>(
			this Stack<T> stack,
			out T element,
			int offset
		)
		{
			element = default;

			if (offset < stack.Count)
			{
				element = stack.ElementAt(offset);
				return true;
			}
			
			return false;
		}
		
		public static T[] PeekAll<T>(this Stack<T> stack) => stack.ToArray();
	}
}