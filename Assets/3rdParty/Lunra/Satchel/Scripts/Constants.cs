namespace Lunra.Satchel
{
	public static class Constants
	{
		const string Prefix = "satchel.";
		
		public static readonly ItemKey<int> InstanceCount = new ItemKey<int>(Prefix + "instance_count");
		public static readonly ItemKey<bool> IgnoreCleanup = new ItemKey<bool>(Prefix + "ignore_cleanup");
		
		public static readonly ItemKey<int> Weight = new ItemKey<int>(Prefix + "weight");
	}
}