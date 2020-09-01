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

	public static class StackRecipeExtensions
	{
		public static StackRecipe ToSingleStackRecipe(
			this PropertyKeyValue[] elements
		)
		{
			return new StackRecipe(
				1,
				elements
			);
		}
		
		public static StackRecipe ToStackRecipe(
			this PropertyKeyValue[] elements,
			int count
		)
		{
			return new StackRecipe(
				count,
				elements
			);
		}
	}
}