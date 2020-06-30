using System;
using Lunra.Core;
using Lunra.StyxMvp.Models;

namespace Lunra.Hothouse.Models
{
	public class InventoryTransaction
	{
		/// <summary>
		/// Creates a new transaction
		/// </summary>
		/// <remarks>
		/// This should only be used by inventory components, so proper registration happens.
		/// </remarks>
		/// <param name="type"></param>
		/// <param name="target"></param>
		/// <param name="items"></param>
		/// <returns></returns>
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

		public override string ToString()
		{
			var result = Type + " to " + Model.ShortenId(Target?.Id);
			result += "\n" + Items;
			return result;
		}
	}
}