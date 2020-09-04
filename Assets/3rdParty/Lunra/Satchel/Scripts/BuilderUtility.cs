using System;

namespace Lunra.Satchel
{
	public class BuilderUtility
	{
		ItemStore itemStore;

		public BuilderUtility(ItemStore itemStore) => this.itemStore = itemStore ?? throw new ArgumentNullException(nameof(itemStore));
		
		public ContainerConstraintBuilder BeginConstraint() => new ContainerConstraintBuilder(itemStore);
		public ContainerFilterBuilder BeginInventoryFilter() => new ContainerFilterBuilder(itemStore);
		public PropertyFilterBuilder BeginPropertyFilter() => new PropertyFilterBuilder(itemStore);
		public ItemBuilder BeginItem() => new ItemBuilder(itemStore);
		
		public Container Container() => new Container(itemStore.IdCounter.Next()).Initialize(itemStore);
	}
}