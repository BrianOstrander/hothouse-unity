using System.Linq;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;

namespace Lunra.Hothouse.Presenters
{
	public class ItemDropPresenter : PrefabPresenter<ItemDropModel, ItemDropView>
	{
		public ItemDropPresenter(GameModel game, ItemDropModel model) : base(game, model) { }

		protected override void Bind()
		{
			Model.Inventory.Changed += OnItemDropInventory;
			
			base.Bind();
		}

		protected override void UnBind()
		{
			Model.Inventory.Changed -= OnItemDropInventory;
			
			base.UnBind();
		}

		protected override void OnViewPrepare()
		{
			base.OnViewPrepare();

			var item = Model.Inventory.Value.Entries.OrderByDescending(i => i.Weight).FirstOrDefault();
			View.SetEntry(item.Weight, item.Type);
		}
		
		#region ItemDropModel Events
		void OnItemDropInventory(Inventory inventory)
		{
			if (inventory.IsEmpty) Game.ItemDrops.InActivate(Model);
		}
		#endregion
	}
}