using System;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
	public interface IAgentInventoryModel : IBaseInventoryModel
	{
		AgentInventoryComponent Inventory { get; }
	}

	public class AgentInventoryComponent : BaseInventoryComponent
	{
		#region Serialized
		#endregion
		
		#region Non Serialized
		#endregion

		public override bool Add(Inventory inventory) => Add(inventory, out _);

		public override bool Add(
			Inventory inventory,
			out Inventory overflow
		)
		{
			overflow = Inventory.Empty;
			if (inventory.IsEmpty) return false;
			
			var hasOverflow = AllCapacity.Value.AddClamped(
				All.Value,
				inventory,
				out var allReplacement,
				out overflow
			);

			AllListener.Value = allReplacement;

			return hasOverflow;
		}

		public override bool Remove(Inventory inventory) => Remove(inventory, out _);
		
		public override bool Remove(
			Inventory inventory,
			out Inventory overflow
		)
		{
			overflow = Inventory.Empty;
			if (inventory.IsEmpty) return false;

			var hasIntersection = All.Value.Intersects(
				inventory,
				out var maximumAvailableForRemoval
			);

			if (!hasIntersection)
			{
				overflow = inventory;
				return false;
			}

			overflow = inventory - maximumAvailableForRemoval;

			AllListener.Value -= maximumAvailableForRemoval;

			return !overflow.IsEmpty;
		}
		
		public void Reset(InventoryCapacity capacity)
		{
			ResetId();
			
			AllListener.Value = Inventory.Empty;
			AllCapacityListener.Value = capacity;
		}

		public override string ToString()
		{
			var result = "Inventory Component [ " + ShortId + " ]:\n";
			foreach (var itemType in Inventory.ValidTypes)
			{
				result += "\n - " + itemType + "\t";

				switch (itemType)
				{
					case Inventory.Types.Scrap:
						result += "\t";
						break;
				}
				
				result += All.Value[itemType]+" / "+AllCapacity.Value.GetMaximumFor(itemType);
			}

			return result;
		}
	}
}