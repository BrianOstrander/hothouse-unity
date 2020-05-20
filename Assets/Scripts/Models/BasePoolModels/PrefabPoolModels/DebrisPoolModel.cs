using System;
using System.Collections.Generic;
using Lunra.Core;
using Lunra.Hothouse.Presenters;
using Lunra.Hothouse.Views;
using Lunra.NumberDemon;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class DebrisPoolModel : BasePrefabPoolModel<ClearableModel>
	{
		static readonly string[] ValidPrefabIds = new[]
		{
			"debris_small",
			"debris_large"
		};
		
		static class Defaults
		{
			public const int ReproductionFailureLimit = 40;

			public static Func<Inventory> GenerateDropsEmpty() => () => Inventory.Empty;
			
			public static Func<Inventory> GenerateDrops(
				Inventory.Types type,
				int weight = 1
			)
			{
				return () => new Inventory(
					new Dictionary<Inventory.Types, int>
					{
						{ type , weight }
					}
				);
			}
		}
		
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

		void Reset(
			ClearableModel model
		)
		{
			model.HealthMaximum.Value = 10f;
			model.Health.Value = 10f;
			model.ClearancePriority.Value = null;
			model.ItemDrops.Value = new Inventory(
				new Dictionary<Inventory.Types, int>
				{
					{ Inventory.Types.Scrap, 1 }
				}
			);
		}
	}
}