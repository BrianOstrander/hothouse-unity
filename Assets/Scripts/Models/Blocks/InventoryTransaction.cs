using System;

namespace Lunra.Hothouse.Models
{
	public class InventoryTransaction
	{
		public static InventoryTransaction RequestDeliver(
			IBaseInventoryComponent target,
			Inventory items
		)
		{
			return new InventoryTransaction(
				Types.Deliver,
				States.Request,
				InstanceId.New(target),
				items
			);
		}
		
		public static InventoryTransaction RequestDistribute(
			IBaseInventoryComponent target,
			Inventory items
		)
		{
			return new InventoryTransaction(
				Types.Distribute,
				States.Request,
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

		public enum States
		{
			Unknown = 0,
			Request = 10,
			Load = 20,
			Unload = 30,
			Complete = 40
		}

		public Types Type { get; }
		public States State { get; }
		public InstanceId Target { get; }
		public Inventory Items { get; }

		
		InventoryTransaction(
			Types type,
			States state,
			InstanceId target,
			Inventory items
		)
		{
			if (target.IsNull) throw new ArgumentException("Cannot have a null instance: " + target);
			
			Type = type;
			State = state;
			Target = target;
			Items = items;
		}
		
		public InventoryTransaction New(
			Types? type = null,
			States? state = null,
			InstanceId target = null,
			Inventory? items = null
		)
		{
			return new InventoryTransaction(
				type ?? Type,
				state ?? State,
				target ?? Target,
				items ?? Items
			);
		}
	}
}