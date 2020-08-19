namespace Lunra.Satchel
{
	public static class Constants
	{
		const string Prefix = "satchel.";
		
		public static readonly ItemKey<int> Weight = new ItemKey<int>(Prefix + "weight");
	}
}