using System.Linq;
using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Views;

namespace Lunra.WildVacuum.Presenters
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
			var item = Model.Inventory.Value.Current.OrderByDescending(i => i.Count).FirstOrDefault();
			View.SetEntry(item.Count, item.Type);
		}
		
		#region ItemDropModel Events
		void OnItemDropInventory(Inventory inventory)
		{
			if (inventory.IsEmpty) Game.ItemDrops.InActivate(Model);
		}
		#endregion
	}
}