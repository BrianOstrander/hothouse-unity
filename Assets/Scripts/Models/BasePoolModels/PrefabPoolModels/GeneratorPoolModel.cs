using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Presenters;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class GeneratorPoolModel : BasePrefabPoolModel<GeneratorModel>
	{
		GameModel game;
		
		public override void Initialize(GameModel game)
		{
			this.game = game;
			Initialize(
				model => new GeneratorPresenter(game, model)
			);
		}

		public GeneratorModel Activate(
			DecorationModel parent,
			Vector3 position,
			Quaternion rotation,
			FloatRange refillDurationRange,
			FloatRange expireDurationRange,
			params (Inventory.Types Type, int Minimum, int Maximum)[] items
		)
		{
			var result = Activate(
				"source_entrance",
				parent.RoomTransform.Id.Value,
				position,
				rotation,
				model => Reset(
					model,
					parent,
					refillDurationRange,
					expireDurationRange,
					items
				)
			);
			if (IsInitialized) game.LastLightUpdate.Value = game.LastLightUpdate.Value.SetSensitiveStale(result.Id.Value);
			return result;
		}
		
		void Reset(
			GeneratorModel model,
			DecorationModel parent,
			FloatRange refillDurationRange,
			FloatRange expireDurationRange,
			params (Inventory.Types Type, int Minimum, int Maximum)[] items
		)
		{
			model.Parent.Value = InstanceId.New(parent);
		
			model.LightSensitive.Reset();
			model.Enterable.Reset();
			model.Inventory.Reset(
				InventoryPermission.WithdrawalForJobs(Jobs.Stockpiler),
				InventoryCapacity.ByIndividualWeight(
					Inventory.FromEntries(
						items.Select(i => (i.Type, i.Maximum)).ToArray()	
					)	
				)
			);

			model.Inventory.Desired.Value = InventoryDesire.UnCalculated(Inventory.Empty);
			
			model.Generator.Reset(
				refillDurationRange,
				expireDurationRange,
				items
			);
		}
	}
}