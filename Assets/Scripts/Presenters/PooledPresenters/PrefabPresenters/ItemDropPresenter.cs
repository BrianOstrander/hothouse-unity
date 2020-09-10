using System.Linq;
using Lunra.Core;
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
			foreach (var itemDelta in delta.ItemEvents)
			{
				// I think we can safely ignore items not found as having been destroyed...
				if (Game.Items.TryGet(itemDelta.Id, out var item))
				{
					if (!item.TryGet(Items.Keys.Capacity.CurrentCount, out var currentCount) || currentCount != 0) return;
				}
			}
			
			// Making it this far means every capacity is zero...

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