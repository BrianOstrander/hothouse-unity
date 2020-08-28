using Lunra.Core;

namespace Lunra.Satchel
{
	public static class Constants
	{
		const string Prefix = "satchel.";
		
		public static readonly PropertyKey<int> InstanceCount = new PropertyKey<int>(Prefix + nameof(InstanceCount).ToSnakeCase());
		public static readonly PropertyKey<long> InventoryId = new PropertyKey<long>(Prefix + nameof(InventoryId).ToSnakeCase());
		public static readonly PropertyKey<bool> IgnoreCleanup = new PropertyKey<bool>(Prefix + nameof(IgnoreCleanup).ToSnakeCase());
		public static readonly PropertyKey<bool> Destroyed = new PropertyKey<bool>(Prefix + nameof(Destroyed).ToSnakeCase());
	}
}