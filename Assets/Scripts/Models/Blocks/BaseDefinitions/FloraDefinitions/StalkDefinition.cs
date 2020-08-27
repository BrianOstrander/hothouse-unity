using Lunra.Core;

namespace Lunra.Hothouse.Models
{
	public class StalkDefinition : FloraDefinition
	{
		public override IntegerRange ClusterPerRoom => new IntegerRange(0, 12);
		public override IntegerRange CountPerCluster => new IntegerRange(10, 30);
	}
}