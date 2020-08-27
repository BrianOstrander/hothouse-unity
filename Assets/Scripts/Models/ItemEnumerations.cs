using Lunra.Core;

namespace Lunra.Hothouse.Models
{
	public static class ItemEnumerations
	{
		public static class Resource
		{
			public static class Logistics
			{
				public static class Types
				{
					public static readonly string None = nameof(None).ToSnakeCase();
					public static readonly string Construction = nameof(Construction).ToSnakeCase();
					public static readonly string Recipe = nameof(Recipe).ToSnakeCase();
					public static readonly string Goal = nameof(Goal).ToSnakeCase();
				}
				
				public static class States
				{
					public static readonly string None = nameof(None).ToSnakeCase();
					public static readonly string Input = nameof(Input).ToSnakeCase();
					public static readonly string Output = nameof(Output).ToSnakeCase();
					public static readonly string Transit = nameof(Transit).ToSnakeCase();
					public static readonly string Consumed = nameof(Consumed).ToSnakeCase();
				}
			}
		}
	}
}