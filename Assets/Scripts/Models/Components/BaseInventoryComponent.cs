using Lunra.StyxMvp.Models;
using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
	public interface IBaseInventoryComponent
	{
		ReadonlyProperty<Inventory> All { get; }
		ReadonlyProperty<InventoryCapacity> Capacity { get; }
		
		bool Add(Inventory inventory, out Inventory overflow);
		bool Remove(Inventory inventory);
		bool Remove(Inventory inventory, out Inventory overflow);

		bool IsFull();
		bool IsNotFull();
	}
	
	public interface IBaseInventoryModel : IRoomTransformModel
	{
		IBaseInventoryComponent[] Inventories { get; }
	}

	public abstract class BaseInventoryComponent : Model,
		IBaseInventoryComponent
	{
		#region Serialized
		[JsonProperty] Inventory all = Inventory.Empty;
		protected readonly ListenerProperty<Inventory> allListener;
		[JsonIgnore] public ReadonlyProperty<Inventory> All { get; }
		
		[JsonProperty] InventoryCapacity capacity = InventoryCapacity.None();
		protected readonly ListenerProperty<InventoryCapacity> capacityListener;
		[JsonIgnore] public ReadonlyProperty<InventoryCapacity> Capacity { get; }
		#endregion
		
		#region Non Serialized
		#endregion

		public BaseInventoryComponent()
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

		public abstract bool Add(Inventory inventory);
		public abstract bool Add(Inventory inventory, out Inventory overflow);
		public abstract bool Remove(Inventory inventory);
		public abstract bool Remove(Inventory inventory, out Inventory overflow);
	}
}