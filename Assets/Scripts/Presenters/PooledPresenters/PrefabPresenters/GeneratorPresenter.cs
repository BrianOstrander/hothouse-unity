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

			Model.Inventory.All.Changed += OnGeneratorInventoryAll;
			Model.Generator.Rate.Changed += OnGeneratorRate;
			
			base.Bind();
		}

		protected override void UnBind()
		{
			Game.SimulationUpdate -= GameSimulationUpdate;
			
			Model.Inventory.All.Changed -= OnGeneratorInventoryAll;
			Model.Generator.Rate.Changed -= OnGeneratorRate;
		
			base.UnBind();
		}
		
		#region GameModel Events
		void GameSimulationUpdate() => Model.Generator.Update(Game, Model);
		#endregion

		#region GeneratorModel Events
		void OnGeneratorInventoryAll(Inventory all) => Model.Generator.CalculateRate(Model);

		void OnGeneratorRate(float rate)
		{
			if (!Model.Parent.Value.TryGetInstance<DecorationModel>(Game, out var parent))
			{
				Debug.LogError("Trying to update flow rate but parent could not be found");
				return;
			}

			parent.Flow.Value = rate;
		}
		#endregion
	}
}