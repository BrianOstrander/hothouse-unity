namespace Lunra.Hothouse.Models
{
	public class StalkDefinition : FloraDefinition
	{
		public override Inventory.Types Seed => Inventory.Types.StalkSeed;

		public override (Inventory.Types Type, int Minimum, int Maximum)[] ItemDrops => new[]
		{
			(Inventory.Types.StalkSeed, 0, 2),
			(StalkRaw: Inventory.Types.Stalk, 1, 1)
		};
	}
}