using System.Linq;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;

namespace Lunra.Hothouse.Presenters
{
	public class ItemDropPresenter : PooledPresenter<ItemDropModel, ItemDropView>
	{
		public ItemDropPresenter(GameModel game, ItemDropModel model) : base(game, model) { }

		protected override void Bind()
		{
			base.Bind();
			
			Model.Inventory.Changed += OnItemDropInventory;
		}

		protected override void UnBind()
		{
			base.UnBind();
			
			Model.Inventory.Changed -= OnItemDropInventory;
		}

		protected override void OnViewPrepare()
		{
			var item = Model.Inventory.Value.Entries.OrderByDescending(i => i.Value).FirstOrDefault();
			View.SetEntry(item.Value, item.Key);
		}
		
		#region ItemDropModel Events
		void OnItemDropInventory(Inventory inventory)
		{
			if (inventory.IsEmpty) Game.ItemDrops.InActivate(Model);
		}
		#endregion
	}
}