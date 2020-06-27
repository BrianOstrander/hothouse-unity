using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using UnityEngine;

namespace Lunra.Hothouse.Presenters
{
	public class ItemDropPresenter : PrefabPresenter<ItemDropModel, ItemDropView>
	{
		public ItemDropPresenter(GameModel game, ItemDropModel model) : base(game, model) { }

		protected override void Bind()
		{
			Game.NavigationMesh.CalculationState.Changed += OnNavigationMeshCalculationState;
			
			Model.Inventory.All.Changed += OnItemDropInventory;
			Model.LightSensitive.LightLevel.Changed += OnLightSensitiveLightLevel;
			
			base.Bind();
		}

		protected override void UnBind()
		{
			Game.NavigationMesh.CalculationState.Changed -= OnNavigationMeshCalculationState;
			
			Model.Inventory.All.Changed -= OnItemDropInventory;
			Model.LightSensitive.LightLevel.Changed -= OnLightSensitiveLightLevel;
			
			base.UnBind();
		}

		protected override void OnViewPrepare()
		{
			base.OnViewPrepare();

			var item = Model.Inventory.All.Value.Entries.OrderByDescending(i => i.Weight).FirstOrDefault();
			View.SetEntry(item.Weight, item.Type);
			
			Model.Enterable.Entrances.Value = 
				new Entrance(
					Model.Transform.Position.Value,
					Vector3.forward,
					false,
					Entrance.States.Unknown
				)
				.ToEnumerable().ToArray();
			
			Model.RecalculateEntrances();
		}
		
		#region ItemDropModel Events
		void OnItemDropInventory(Inventory inventory)
		{
			if (inventory.IsEmpty) Game.ItemDrops.InActivate(Model);
		}
		#endregion

		void OnNavigationMeshCalculationState(NavigationMeshModel.CalculationStates calculationState)
		{
			if (calculationState == NavigationMeshModel.CalculationStates.Completed) Model.RecalculateEntrances();
		}
		
		void OnLightSensitiveLightLevel(float lightLevel) => Model.RecalculateEntrances();
	}
}