using System;

namespace Lunra.Hothouse.Models
{
	public class InventoryTransaction
	{
		public static InventoryTransaction New(
			Types type,
			IBaseInventoryComponent target,
			Inventory items
		)
		{
			return new InventoryTransaction(
				type,
				InstanceId.New(target),
				items
			);
		}

		public enum Types
		{
			Unknown = 0,
			Deliver = 10,
			Distribute = 20
		}
		
		public Types Type { get; }
		public InstanceId Target { get; }
		public Inventory Items { get; }

		
		InventoryTransaction(
			Types type,
			InstanceId target,
			Inventory items
		)
		{
			if (target.IsNull) throw new ArgumentException("Cannot have a null instance: " + target);
			
			Type = type;
			Target = target;
			Items = items;
		}
		
		public InventoryTransaction New(
			Types? type = null,
			InstanceId target = null,
			Inventory? items = null
		)
		{
			return new InventoryTransaction(
				type ?? Type,
				target ?? Target,
				items ?? Items
			);
		}
	}
}