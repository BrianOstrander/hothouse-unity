namespace Lunra.Satchel
{
	[Processor]
	public abstract class Processor
	{
		public virtual int Priority => 0;

		protected ItemStore ItemStore { get; private set; }
		protected PropertyFilter Filter { get; private set; }
		
		public Processor Initialize(ItemStore itemStore)
		{
			ItemStore = itemStore;

			Filter = GetFilter();

			return this;
		}

		protected virtual PropertyFilter GetFilter() => new PropertyFilterBuilder(ItemStore);

		public virtual bool IsValid(Item item) => Filter.Validate(item);

		public abstract void Process(Item item);
	}
}