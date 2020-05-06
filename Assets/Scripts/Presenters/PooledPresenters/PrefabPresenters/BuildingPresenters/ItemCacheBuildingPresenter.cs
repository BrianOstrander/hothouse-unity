using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Presenters;
using Lunra.WildVacuum.Views;
using UnityEngine;

namespace System
{
	public class ItemCacheBuildingPresenter : BuildingPresenter<ItemCacheBuildingModel, ItemCacheBuildingView>
	{
		public ItemCacheBuildingPresenter(GameModel game, ItemCacheBuildingModel model) : base(game, model) { }

		protected override void Bind()
		{
			base.Bind();
			
			Model.Inventory.Changed += value => Debug.Log("ItemCache:\n" + value);

			Model.Inventory.Changed += OnItemCacheBuildingInventory;
		}

		protected override void UnBind()
		{
			base.UnBind();
			
			Model.Inventory.Changed -= OnItemCacheBuildingInventory;
		}

		protected override void OnViewShown()
		{
			base.OnViewShown();
			OnItemCacheBuildingInventory(Model.Inventory.Value);
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