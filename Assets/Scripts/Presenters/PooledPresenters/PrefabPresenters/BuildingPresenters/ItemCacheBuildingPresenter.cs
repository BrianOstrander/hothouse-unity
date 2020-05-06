using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Presenters;
using Lunra.WildVacuum.Views;
using UnityEngine;

namespace System
{
	public class ItemCacheBuildingPresenter : BuildingPresenter<ItemCacheBuildingView, ItemCacheBuildingModel>
	{
		public ItemCacheBuildingPresenter(GameModel game, ItemCacheBuildingModel model) : base(game, model)
		{
			model.Inventory.Changed += value => Debug.Log("ItemCache:\n" + value);

			model.Inventory.Changed += OnItemCacheBuildingInventory;
		}

		protected override void OnUnBind()
		{
			base.OnUnBind();

			Model.Inventory.Changed -= OnItemCacheBuildingInventory;
		}

		protected override void OnShow()
		{
			View.Shown += () => OnItemCacheBuildingInventory(Model.Inventory.Value);
		}
		
		#region ItemCacheBuildingModel Events
		void OnItemCacheBuildingInventory(Inventory inventory)
		{
			if (View.NotVisible) return;

			View.Text = inventory[Item.Types.Stalks] + " / " + inventory.GetMaximum(Item.Types.Stalks);
		}
		#endregion
	}
}