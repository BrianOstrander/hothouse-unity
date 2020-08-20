using System;

namespace Lunra.Satchel
{
	public interface IItemModifier
	{
		bool IsValid(Item item);
		void Apply(Item item);
	}
	
	public class CallbackItemModifier : IItemModifier
	{
		Func<Item, bool> isValid;
		Action<Item> apply;
		
		public CallbackItemModifier(
			Func<Item, bool> isValid,
			Action<Item> apply
		)
		{
			this.isValid = isValid;
			this.apply = apply ?? throw new ArgumentNullException(nameof(apply));
		}
		
		public CallbackItemModifier(Action<Item> apply) : this(null, apply) { }

		public bool IsValid(Item item) => isValid == null || isValid(item);

		public void Apply(Item item) => apply(item);
	}
	
	public abstract class ItemModifier : IItemModifier
	{
		public virtual bool IsValid(Item item) => true;

		public abstract void Apply(Item item);
	}
}