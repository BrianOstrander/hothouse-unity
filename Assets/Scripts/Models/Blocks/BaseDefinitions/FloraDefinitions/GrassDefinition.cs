using Lunra.Core;

namespace Lunra.Hothouse.Models
{
	public class GrassDefinition : FloraDefinition
	{
		public override (Inventory.Types Type, int Minimum, int Maximum)[] ItemDrops => new[]
		{
			(SweetStalkRaw: Inventory.Types.Grass, 1, 1)
		};

		public override IntegerRange ClusterPerRoom => new IntegerRange(0, 10);
		public override IntegerRange CountPerCluster => new IntegerRange(20, 30);
		public override FloatRange ReproductionDuration => new FloatRange(4f, 7f);
		public override FloatRange ReproductionRadius => new FloatRange(1f, 2f);
		public override float HealthMaximum => 10f;
	}
}