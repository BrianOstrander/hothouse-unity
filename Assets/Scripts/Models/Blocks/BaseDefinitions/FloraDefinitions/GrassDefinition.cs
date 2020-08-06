namespace Lunra.Hothouse.Models
{
	public class GrassDefinition : FloraDefinition
	{
		public override (Inventory.Types Type, int Minimum, int Maximum)[] ItemDrops => new[]
		{
			(SweetStalkRaw: Inventory.Types.Grass, 1, 1)
		};
	}
}