using System;

namespace Lunra.Satchel
{
	public class BuilderUtility
	{
		ItemStore itemStore;

		public BuilderUtility(ItemStore itemStore) => this.itemStore = itemStore ?? throw new ArgumentNullException(nameof(itemStore));
		
		public InventoryConstraintBuilder BeginConstraint() => new InventoryConstraintBuilder(itemStore);
		public InventoryFilterBuilder BeginFilter() => new InventoryFilterBuilder(itemStore);
	}
}