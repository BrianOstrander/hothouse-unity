namespace Lunra.Satchel
{
	public static class Constants
	{
		const string Prefix = "satchel.";
		
		public static readonly PropertyKey<int> InstanceCount = new PropertyKey<int>(Prefix + "instance_count");
		public static readonly PropertyKey<bool> IgnoreCleanup = new PropertyKey<bool>(Prefix + "ignore_cleanup");
	}
}