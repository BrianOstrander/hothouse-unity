using Lunra.Hothouse.Models;
using UnityEngine;

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
		
		/// <summary>
		/// If it's possible that this processor could destroy the item, it should return true to indicate an additional
		/// destruction check is required. 
		/// </summary>
		/// <returns><c>true</c> if destroyed, <c>false</c> otherwise.</returns>
		public virtual bool IsDestructionPossible() => false;
		
		public abstract void Process(Item item, float deltaTime);
	}
}