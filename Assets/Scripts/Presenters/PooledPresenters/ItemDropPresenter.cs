using System.Linq;
using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Views;
using UnityEngine;

namespace Lunra.WildVacuum.Presenters
{
	public class ItemDropPresenter : PooledPresenter<ItemDropModel, ItemDropView>
	{
		public ItemDropPresenter(GameModel game, ItemDropModel model) : base(game, model) { }

		protected override void OnBind()
		{
			base.OnBind();

			Model.Inventory.Changed += OnItemDropInventory;
		}

		protected override void OnUnBind()
		{
			base.OnUnBind();
			
			Model.Inventory.Changed -= OnItemDropInventory;
		}

		protected override void OnShow()
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