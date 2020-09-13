using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using Lunra.Satchel;
using UnityEngine;

namespace Lunra.Hothouse.Presenters
{
	public class ItemDropPresenter : PrefabPresenter<ItemDropModel, ItemDropView>
	{
		public ItemDropPresenter(GameModel game, ItemDropModel model) : base(game, model) { }

		protected override void Bind()
		{
			Game.NavigationMesh.CalculationState.Changed += OnNavigationMeshCalculationState;

			Model.Inventory.Container.UpdatedItem += OnItemDropInventoryUpdateItem;
			Model.LightSensitive.LightLevel.Changed += OnLightSensitiveLightLevel;
			
			base.Bind();
		}

		protected override void UnBind()
		{
			Game.NavigationMesh.CalculationState.Changed -= OnNavigationMeshCalculationState;
			
			Model.Inventory.Container.UpdatedItem -= OnItemDropInventoryUpdateItem;
			Model.LightSensitive.LightLevel.Changed -= OnLightSensitiveLightLevel;
			
			base.UnBind();
		}

		protected override void OnViewPrepare()
		{
			base.OnViewPrepare();

			Debug.LogWarning("TODO: Set inventory weight stuff");
			// var item = Model.Inventory.All.Value.Entries.OrderByDescending(i => i.Weight).FirstOrDefault();
			// View.SetEntry(item.Weight);
			
			Model.RecalculateEntrances(Model.Transform.Position.Value);
		}
		
		#region ItemDropModel Events
		void OnItemDropInventoryUpdateItem(ItemStore.Event delta)
		{
			return;
			
			// TODO: Figure out how to destroy this when all reasources have been taken...
			
			foreach (var entry in Model.Inventory.Container.All())
			{
				if (entry.Item.TryGet(Items.Keys.Capacity.Desire, out var desire) && desire == Items.Values.Capacity.Desires.NotCalculated) return;
				if (entry.Item.TryGet(Items.Keys.Capacity.CountCurrent, out var currentCount) && 0 < currentCount) return;
			}
			
			// If we make it here, that means there was no capacity with a current count greater than zero.

			Model.Inventory.Container.DestroyAll();
			Model.PooledState.Value = PooledStates.InActive;
		}
		#endregion

		void OnNavigationMeshCalculationState(NavigationMeshModel.CalculationStates calculationState)
		{
			if (calculationState == NavigationMeshModel.CalculationStates.Completed) Model.RecalculateEntrances();
		}
		
		void OnLightSensitiveLightLevel(float lightLevel) => Model.RecalculateEntrances();
	}
}