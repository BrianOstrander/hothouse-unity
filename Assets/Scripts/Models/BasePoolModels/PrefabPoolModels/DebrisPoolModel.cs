using System;
using System.Collections.Generic;
using Lunra.Core;
using Lunra.Hothouse.Presenters;
using Lunra.Hothouse.Views;
using Lunra.NumberDemon;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class DebrisPoolModel : BasePrefabPoolModel<ClearableModel_old>
	{
		static readonly string[] ValidPrefabIds = new[]
		{
			"debris_small",
			"debris_large"
		};
		
		GameModel game;
		
		public override void Initialize(GameModel game)
		{
			this.game = game;
			Initialize(
				model => new ClearablePresenter<ClearableModel_old, ClearableView>(game, model)	
			);
		}

		public ClearableModel_old Activate(
			string roomId,
			Vector3 position
		)
		{
			var result = Activate(
				ValidPrefabIds.Random(),
				roomId,
				position,
				RandomRotation,
				model => Reset(model)
			);
			if (IsInitialized) game.LastLightUpdate.Value = game.LastLightUpdate.Value.SetSensitiveStale(result.Id.Value);
			return result;
		}
		
		Quaternion RandomRotation => Quaternion.AngleAxis(DemonUtility.GetNextFloat(0f, 360f), Vector3.up);

		void Reset(
			ClearableModel_old model
		)
		{
			model.Clearable.Reset();
			model.Health.ResetToMaximum(10f);
			model.Clearable.ItemDrops.Value = new Inventory(
				new Dictionary<Inventory.Types, int>
				{
					{ Inventory.Types.Scrap, 1 }
				}
			);
		}
	}
}