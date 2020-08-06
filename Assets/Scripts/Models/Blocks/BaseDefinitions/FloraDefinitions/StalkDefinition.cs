using Lunra.Core;

namespace Lunra.Hothouse.Models
{
	public class StalkDefinition : FloraDefinition
	{
		public override (Inventory.Types Type, int Minimum, int Maximum)[] ItemDrops => new[]
		{
			(StalkRaw: Inventory.Types.Stalk, 1, 1)
		};
		
		public override IntegerRange ClusterPerRoom => new IntegerRange(0, 12);
		public override IntegerRange CountPerCluster => new IntegerRange(10, 30);
	}
}