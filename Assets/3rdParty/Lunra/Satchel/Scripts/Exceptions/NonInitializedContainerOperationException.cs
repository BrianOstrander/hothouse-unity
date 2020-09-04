using System;

namespace Lunra.Satchel
{
	public class NonInitializedContainerOperationException : Exception
	{
		public NonInitializedContainerOperationException(string operation) : base($"Attempted operation \"{operation}\" on an inventory that has not been initialized") {}
	}
}