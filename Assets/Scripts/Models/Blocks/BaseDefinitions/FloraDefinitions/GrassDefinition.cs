namespace Lunra.Hothouse.Models
{
	public class GrassDefinition : FloraDefinition
	{
		public override Inventory.Types Seed => Inventory.Types.GrassSeed;

		public override (Inventory.Types Type, int Minimum, int Maximum)[] ItemDrops => new[]
		{
			(SweetStalkSeed: Inventory.Types.GrassSeed, 0, 2),
			(SweetStalkRaw: Inventory.Types.Grass, 1, 1)
		};
	}
}