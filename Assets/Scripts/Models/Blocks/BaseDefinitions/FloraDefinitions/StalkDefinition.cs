namespace Lunra.Hothouse.Models
{
	public class StalkDefinition : FloraDefinition
	{
		public override (Inventory.Types Type, int Minimum, int Maximum)[] ItemDrops => new[]
		{
			(StalkRaw: Inventory.Types.Stalk, 1, 1)
		};
	}
}