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
			
			// Model.Inventory.All.Changed += OnItemDropInventory;
			Debug.LogWarning("TODO: Bind Inventory");
			Model.LightSensitive.LightLevel.Changed += OnLightSensitiveLightLevel;
			
			base.Bind();
		}

		protected override void UnBind()
		{
			Game.NavigationMesh.CalculationState.Changed -= OnNavigationMeshCalculationState;
			
			// Model.Inventory.All.Changed -= OnItemDropInventory;
			Debug.LogWarning("TODO: UnBind Inventory");
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
		void OnItemDropInventory(Container.Event delta)
		{
			if (delta.IsEmpty) Game.ItemDrops.InActivate(Model);
		}
		#endregion

		void OnNavigationMeshCalculationState(NavigationMeshModel.CalculationStates calculationState)
		{
			if (calculationState == NavigationMeshModel.CalculationStates.Completed) Model.RecalculateEntrances();
		}
		
		void OnLightSensitiveLightLevel(float lightLevel) => Model.RecalculateEntrances();
	}
}