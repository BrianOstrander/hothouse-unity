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
		public Inventory Container { get; private set; }
		#endregion
		
		#region Non Serialized
		#endregion

		public override void UnBind()
		{
			// I think this is the correct place to destroy any items, should ensure they are properly cleaned up...
			Container.Destroy();
			Container = null;
		}

		public override void Initialize(GameModel game, IParentComponentModel model)
		{
			base.Initialize(game, model);

			Container = Container?.Initialize(game.Items) ?? game.Items.Builder.Inventory();
		}

		public void Reset(ItemStore itemStore)
		{
			ResetId();

			// TODO: This should eventually only be handled by initialize
			// Should I destroy this inventory and create a new one so we are sure the Id is wiped out?
			// Probably not since unbind destroys it... but that seems non-intuitive...
			Container = Container?.Initialize(itemStore) ?? itemStore.Builder.Inventory();
		}

		public override string ToString()
		{
			var result = "Inventory Component [ " + ShortId + " ]:\n";
			result += Container.ToString(Inventory.Formats.IncludeItems | Inventory.Formats.IncludeItemProperties);
			return result;
		}
	}
}