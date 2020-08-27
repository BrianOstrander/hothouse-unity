using Lunra.Core;
using Lunra.Satchel;

namespace Lunra.Hothouse.Models
{
	public static class ItemKeys
	{
		static PropertyKey<T> Create<T>(params string[] elements) => new PropertyKey<T>(elements.ToPunctualSnakeCase());
		
		public static class Resource
		{
			static PropertyKey<T> Create<T>(string suffix) => ItemKeys.Create<T>(nameof(Resource), suffix);

			public static readonly PropertyKey<string> Id = Create<string>(nameof(Id));
			public static readonly PropertyKey<string> InventoryId = Create<string>(nameof(InventoryId));

			public static class Logistics
			{
				static PropertyKey<T> Create<T>(string suffix) => ItemKeys.Create<T>(nameof(Resource), nameof(Logistics), suffix);
				
				public static readonly PropertyKey<string> Type = Create<string>(nameof(Type));
				public static readonly PropertyKey<string> State = Create<string>(nameof(State));
				public static readonly PropertyKey<int> Available = Create<int>(nameof(Available));
				public static readonly PropertyKey<int> Promised = Create<int>(nameof(Promised));
			}
			
			public static class Decay
			{
				static PropertyKey<T> Create<T>(string suffix) => ItemKeys.Create<T>(nameof(Resource), nameof(Decay), suffix);
				
				public static readonly PropertyKey<bool> Enabled = Create<bool>(nameof(Enabled));
				public static readonly PropertyKey<bool> ForbidDestruction = Create<bool>(nameof(ForbidDestruction));
				public static readonly PropertyKey<float> Maximum = Create<float>(nameof(Maximum));
				public static readonly PropertyKey<float> Current = Create<float>(nameof(Current));
				public static readonly PropertyKey<float> Previous = Create<float>(nameof(Previous));
				public static readonly PropertyKey<float> Rate = Create<float>(nameof(Rate));
				public static readonly PropertyKey<float> RatePredicted = Create<float>(nameof(RatePredicted));
			}
		}
	}
}