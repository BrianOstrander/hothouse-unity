using System;

namespace Lunra.Satchel
{
	public class BuilderUtility
	{
		ItemStore itemStore;

		public BuilderUtility(ItemStore itemStore) => this.itemStore = itemStore ?? throw new ArgumentNullException(nameof(itemStore));
		
		public InventoryConstraintBuilder BeginConstraint() => new InventoryConstraintBuilder(itemStore);
		public InventoryFilterBuilder BeginInventoryFilter() => new InventoryFilterBuilder(itemStore);
		public PropertyFilterBuilder BeginPropertyFilter() => new PropertyFilterBuilder(itemStore);
		public ItemBuilder BeginItem() => new ItemBuilder(itemStore);
		
		public Inventory Inventory() => new Inventory(itemStore.IdCounter.Next()).Initialize(itemStore);
	}
}