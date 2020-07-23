namespace Lunra.Hothouse.Models
{
	public class SweetStalkDefinition : FloraDefinition
	{
		public override Inventory.Types Seed => Inventory.Types.SweetStalkSeed;

		public override (Inventory.Types Type, int Minimum, int Maximum)[] ItemDrops => new[]
		{
			(Inventory.Types.SweetStalkSeed, 0, 2),
			(Inventory.Types.SweetStalkRaw, 1, 1)
		};
	}
}