using System;
using System.Collections.Generic;
using Lunra.Core;
using Lunra.Hothouse.Presenters;
using Lunra.Hothouse.Views;
using Lunra.NumberDemon;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class DebrisPoolModel : BasePrefabPoolModel<DebrisModel>
	{
		public readonly string[] ValidPrefabIds =
		{
			"debris_small",
			"debris_large"
		};
		
		GameModel game;
		
		public override void Initialize(GameModel game)
		{
			this.game = game;
			Initialize(
				model => new ClearablePresenter<DebrisModel, ClearableView>(game, model)	
			);
		}

		public DebrisModel Activate(
			string prefabId,
			string roomId,
			Vector3 position,
			Quaternion rotation
		)
		{
			var result = Activate(
				prefabId,
				roomId,
				position,
				rotation,
				model => Reset(model)
			);
			if (IsInitialized) game.LastLightUpdate.Value = game.LastLightUpdate.Value.SetSensitiveStale(result.Id.Value);
			return result;
		}
		
		void Reset(
			DebrisModel model
		)
		{
			model.Health.ResetToMaximum(10f);
			model.Obligations.Reset();
			model.Enterable.Reset();
			model.Clearable.Reset(
				new Inventory(
					new Dictionary<Inventory.Types, int>
					{
						{ Inventory.Types.Scrap, 1 }
					}
				)	
			);
		}
	}
}