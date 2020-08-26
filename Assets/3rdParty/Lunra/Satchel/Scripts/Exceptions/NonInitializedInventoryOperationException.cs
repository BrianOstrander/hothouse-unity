using System;

namespace Lunra.Satchel
{
	public class NonInitializedInventoryOperationException : Exception
	{
		public NonInitializedInventoryOperationException(string operation) : base($"Attempted operation \"{operation}\" on an inventory that has not been initialized") {}
	}
}