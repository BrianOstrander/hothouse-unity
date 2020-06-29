using Lunra.StyxMvp.Models;
using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
	public interface IAgentInventoryModel : IRoomTransformModel
	{
		AgentInventoryComponent Inventory { get; }
	}

	public class AgentInventoryComponent : Model
	{
		#region Serialized
		[JsonProperty] Inventory all = Inventory.Empty;
		readonly ListenerProperty<Inventory> allListener;
		[JsonIgnore] public ReadonlyProperty<Inventory> All { get; }
		
		[JsonProperty] InventoryCapacity capacity = InventoryCapacity.None();
		readonly ListenerProperty<InventoryCapacity> capacityListener;
		[JsonIgnore] public ReadonlyProperty<InventoryCapacity> Capacity { get; }
		#endregion
		
		#region Non Serialized
		#endregion

		public AgentInventoryComponent()
		{
			All = new ReadonlyProperty<Inventory>(
				value => all = value,
				() => all,
				out allListener
			);
			Capacity = new ReadonlyProperty<InventoryCapacity>(
				value => capacity = value,
				() => capacity,
				out capacityListener
			);
		}

		public bool IsFull() => Capacity.Value.IsFull(All.Value);
		public bool IsNotFull() => !IsFull();

		public bool Add(Inventory inventory) => Add(inventory, out _);

		public bool Add(
			Inventory inventory,
			out Inventory overflow
		)
		{
			overflow = Inventory.Empty;
			if (inventory.IsEmpty) return false;
			
			var hasOverflow = Capacity.Value.AddClamped(
				All.Value,
				inventory,
				out var allReplacement,
				out overflow
			);

			allListener.Value = allReplacement;

			return hasOverflow;
		}

		public bool Remove(Inventory inventory) => Remove(inventory, out _);
		
		public bool Remove(
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

			allListener.Value -= maximumAvailableForRemoval;

			return !overflow.IsEmpty;
		}
		
		public void Reset(InventoryCapacity capacity)
		{
			allListener.Value = Inventory.Empty;
			capacityListener.Value = capacity;
		}

		public override string ToString()
		{
			var result = "Inventory Component:\n";
			foreach (var itemType in Inventory.ValidTypes)
			{
				result += "\n - " + itemType;
				result += "\n\tStored: \t" + All.Value[itemType];
				result += "\n\tCapacity: \t" + Capacity.Value.GetMaximumFor(itemType);
				result += "\n";
			}

			return result;
		}
	}
}