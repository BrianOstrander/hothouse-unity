using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using UnityEngine;

namespace Lunra.Hothouse.Presenters
{
	public class GeneratorPresenter : PrefabPresenter<GeneratorModel, PrefabView>
	{
		public GeneratorPresenter(GameModel game, GeneratorModel model) : base(game, model) { }

		protected override void Bind()
		{
			Game.SimulationUpdate += GameSimulationUpdate;
			Game.NavigationMesh.CalculationState.Changed += OnNavigationMeshCalculationState;

			Model.Inventory.All.Changed += OnGeneratorInventoryAll;
			Model.Generator.Rate.Changed += OnGeneratorGeneratorRate;
			Model.LightSensitive.LightLevel.Changed += OnGeneratorLightSensitiveLightLevel;
			
			base.Bind();
		}

		protected override void UnBind()
		{
			Game.SimulationUpdate -= GameSimulationUpdate;
			Game.NavigationMesh.CalculationState.Changed -= OnNavigationMeshCalculationState;
			
			Model.Inventory.All.Changed -= OnGeneratorInventoryAll;
			Model.Generator.Rate.Changed -= OnGeneratorGeneratorRate;
			Model.LightSensitive.LightLevel.Changed -= OnGeneratorLightSensitiveLightLevel;
		
			base.UnBind();
		}
		
		#region View Events
		protected override void OnViewPrepare()
		{
			base.OnViewPrepare();

			Model.RecalculateEntrances(Model.Transform.Position.Value);
		}
		#endregion
		
		#region GameModel Events
		void GameSimulationUpdate() => Model.Generator.Update(Game, Model);
		#endregion
		
		#region Navigation Events
		void OnNavigationMeshCalculationState(NavigationMeshModel.CalculationStates calculationState)
        {
        	if (calculationState == NavigationMeshModel.CalculationStates.Completed) Model.RecalculateEntrances();
        }
		#endregion

		#region GeneratorModel Events
		void OnGeneratorInventoryAll(Inventory all) => Model.Generator.CalculateRate(Model);

		void OnGeneratorGeneratorRate(float rate)
		{
			if (!Model.Parent.Value.TryGetInstance<DecorationModel>(Game, out var parent))
			{
				Debug.LogError("Trying to update flow rate but parent could not be found");
				return;
			}

			parent.Flow.Value = rate;
		}

		void OnGeneratorLightSensitiveLightLevel(float lightLevel) => Model.RecalculateEntrances();
		#endregion
	}
}