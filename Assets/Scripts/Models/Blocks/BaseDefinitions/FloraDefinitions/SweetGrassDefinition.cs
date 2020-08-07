using Lunra.Core;

namespace Lunra.Hothouse.Models
{
	public class SweetGrassDefinition : FloraDefinition
	{
		public override (Inventory.Types Type, int Minimum, int Maximum)[] ItemDrops => new[]
		{
			(SweetStalkRaw: Inventory.Types.SweetGrass, 4, 6)
		};

		public override IntegerRange ClusterPerRoom => new IntegerRange(0, 3);
		public override IntegerRange CountPerCluster => new IntegerRange(4, 6);
		public override FloatRange ReproductionDuration => new FloatRange(7f, 14f);
		public override FloatRange ReproductionRadius => new FloatRange(1f, 2f);
		public override float HealthMaximum => 25f;
	}
}