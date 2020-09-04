using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Presenters;
using Lunra.Satchel;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class GeneratorPoolModel : BasePrefabPoolModel<GeneratorModel>
	{
		public override void Initialize(GameModel game)
		{
			Initialize(
				game,
				model => new GeneratorPresenter(game, model)
			);
		}

		public GeneratorModel Activate(
			DecorationModel parent,
			Vector3 position,
			Quaternion rotation,
			FloatRange refillDurationRange,
			FloatRange expireDurationRange,
			params (Item Item, int Minimum, int Maximum)[] items
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
			// TODO: Probably need to move this to component initialize...
			if (IsInitialized) Game.LastLightUpdate.Value = Game.LastLightUpdate.Value.SetSensitiveStale(result.Id.Value);
			return result;
		}
		
		void Reset(
			GeneratorModel model,
			DecorationModel parent,
			FloatRange refillDurationRange,
			FloatRange expireDurationRange,
			params (Item Item, int Minimum, int Maximum)[] items
		)
		{
			model.Parent.Value = InstanceId.New(parent);
		
			model.LightSensitive.Reset();
			model.Enterable.Reset();
			
			Debug.LogError("TODO: Handle inventory resetting");
			
			model.Generator.Reset(
				refillDurationRange,
				expireDurationRange,
				items
			);
		}
	}
}