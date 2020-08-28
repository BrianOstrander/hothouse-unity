using Lunra.Satchel;

namespace Lunra.Hothouse.Models
{
	public class StackRecipe
	{
		public int Count { get; private set; }
		public PropertyKeyValue[] Properties { get; private set; }

		public StackRecipe(
			int count,
			params PropertyKeyValue[] properties
		)
		{
			Count = count;
			Properties = properties;
		}
	}
}