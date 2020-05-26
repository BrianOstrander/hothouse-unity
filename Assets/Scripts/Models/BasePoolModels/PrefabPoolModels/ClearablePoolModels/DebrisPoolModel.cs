using System;
using System.Collections.Generic;
using Lunra.Core;
using Lunra.Hothouse.Presenters;
using Lunra.Hothouse.Views;
using Lunra.NumberDemon;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class DebrisPoolModel : ClearablePoolModel<ClearableModel>
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
				model => new ClearablePresenter<ClearableModel, ClearableView>(game, model)	
			);
		}

		public ClearableModel Activate(
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

		protected override void Reset(
			ClearableModel model
		)
		{
			base.Reset(model);
			
			model.HealthMaximum.Value = 10f;
			model.Health.Value = 10f;
			model.ItemDrops.Value = new Inventory(
				new Dictionary<Inventory.Types, int>
				{
					{ Inventory.Types.Scrap, 1 }
				}
			);
		}
	}
}