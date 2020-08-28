using Lunra.Satchel;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public interface IInventoryModel : IRoomTransformModel
	{
		InventoryComponent Inventory { get; }
	}
	
	public class InventoryComponent : ComponentModel<IInventoryModel>
	{
		#region Serialized
		public Inventory Container { get; private set; } = new Inventory();
		#endregion
		
		#region Non Serialized
		#endregion

		public override void UnBind()
		{
			// I think this is the correct place to destroy any items, should ensure they are properly cleaned up...
			Container.Destroy();
		}

		public override void Initialize(GameModel game, IParentComponentModel model)
		{
			base.Initialize(game, model);

			Container.Initialize(game.Items);
		}

		public void Reset(ItemStore itemStore)
		{
			ResetId();

			// TODO: This should eventually only be handled by initialize
			Container.Initialize(itemStore);
		}

		public override string ToString()
		{
			var result = "Inventory Component [ " + ShortId + " ]:\n";
			result += Container.ToString(Inventory.Formats.IncludeItems | Inventory.Formats.IncludeItemProperties);
			return result;
		}
	}
}