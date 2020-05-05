using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Presenters;
using Lunra.WildVacuum.Views;
using UnityEngine;

namespace System
{
	public class ItemCacheBuildingPresenter : BuildingPresenter<ItemCacheBuildingView, ItemCacheBuildingModel>
	{
		public ItemCacheBuildingPresenter(GameModel game, ItemCacheBuildingModel prefab) : base(game, prefab)
		{
			prefab.Inventory.Changed += value => Debug.Log("ItemCache:\n" + value);

			prefab.Inventory.Changed += OnItemCacheBuildingInventory;
		}

		protected override void OnUnBind()
		{
			base.OnUnBind();

			Prefab.Inventory.Changed -= OnItemCacheBuildingInventory;
		}

		protected override void OnShow()
		{
			View.Text = Prefab.Inventory.Value[Item.Types.Stalks].ToString();
		}
		
		#region ItemCacheBuildingModel Events
		void OnItemCacheBuildingInventory(Inventory inventory)
		{
			if (View.NotVisible) return;

			View.Text = inventory[Item.Types.Stalks].ToString();
		}
		#endregion
	}
}