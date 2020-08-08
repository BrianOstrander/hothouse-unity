using System.Collections.Generic;
using System.Linq;
using Lunra.StyxMvp;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
	public interface IBaseInventoryComponent : IComponentModel
	{
		ReadonlyProperty<Inventory> All { get; }
		ReadonlyProperty<InventoryCapacity> AllCapacity { get; }
		
		bool Add(Inventory inventory, out Inventory overflow);
		bool Remove(Inventory inventory);
		bool Remove(Inventory inventory, out Inventory overflow);

		bool IsFull();
		bool IsNotFull();
	}
	
	public interface IBaseInventoryModel : IRoomTransformModel
	{
		[JsonIgnore] IBaseInventoryComponent[] Inventories { get; }
	}

	public abstract class BaseInventoryComponent : ComponentModel<IBaseInventoryModel>,
		IBaseInventoryComponent
	{
		#region Serialized
		[JsonProperty] Inventory all = Inventory.Empty;
		protected readonly ListenerProperty<Inventory> AllListener;
		[JsonIgnore] public ReadonlyProperty<Inventory> All { get; }
		
		[JsonProperty] InventoryCapacity allCapacity = InventoryCapacity.None();
		protected readonly ListenerProperty<InventoryCapacity> AllCapacityListener;
		[JsonIgnore] public ReadonlyProperty<InventoryCapacity> AllCapacity { get; }
		#endregion
		
		#region Non Serialized
		#endregion

		public BaseInventoryComponent()
		{
			All = new ReadonlyProperty<Inventory>(
				value => all = value,
				() => all,
				out AllListener
			);
			AllCapacity = new ReadonlyProperty<InventoryCapacity>(
				value => allCapacity = value,
				() => allCapacity,
				out AllCapacityListener
			);
		}

		public bool IsFull() => AllCapacity.Value.IsFull(All.Value);
		public bool IsNotFull() => !IsFull();
		public float GetNormalizedFull() => 1f - (AllCapacity.Value.GetCapacityFor(All.Value).TotalWeight / (float) AllCapacity.Value.GetMaximum().TotalWeight);

		public abstract bool Add(Inventory inventory);
		public abstract bool Add(Inventory inventory, out Inventory overflow);
		public abstract bool Remove(Inventory inventory);
		public abstract bool Remove(Inventory inventory, out Inventory overflow);

		protected void ResetId() => Id.Value = App.M.CreateUniqueId();
	}
	
	public static class BaseInventoryGameModelExtensions
	{
		public static IEnumerable<IBaseInventoryModel> GetInventoryParents(
			this GameModel game
		)
		{
			return game
				.Dwellers.AllActive
				.Concat<IBaseInventoryModel>(game.Buildings.AllActive)
				.Concat(game.ItemDrops.AllActive)
				.Concat(game.Generators.AllActive);
				
		}
		
		public static IBaseInventoryModel GetInventoryParent(
			this GameModel game,
			string inventoryId
		)
		{
			return game
				.GetInventoryParents()
				.FirstOrDefault(
					m => m.Inventories.Any(i => i.Id.Value == inventoryId)
				);
		}
		
		public static IEnumerable<IBaseInventoryComponent> GetInventories(
			this GameModel game
		)
		{
			return game
				.GetInventoryParents()
				.SelectMany(m => m.Inventories);
		}
	}
}