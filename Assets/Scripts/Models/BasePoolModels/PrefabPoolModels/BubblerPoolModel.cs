using System;
using System.Collections.Generic;
using Lunra.Core;
using Lunra.Hothouse.Presenters;
using Lunra.NumberDemon;
using Lunra.StyxMvp;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class BubblerPoolModel : BasePrefabPoolModel<BubblerModel>
	{
		static readonly string[] ValidPrefabIds =
		{
			"bubbler_0"
		};
		
		GameModel game;
		Demon generator = new Demon();
		
		public override void Initialize(GameModel game)
		{
			this.game = game;
			Initialize(
				model => new BubblerPresenter(game, model)	
			);
		}

		public BubblerModel Activate(
			string roomId,
			Vector3 position,
			Demon generator = null
		)
		{
			var result = Activate(
				ValidPrefabIds.Random(),
				roomId,
				position,
				Quaternion.AngleAxis((generator ?? this.generator).GetNextFloat(0f, 360f), Vector3.up),
				model => Reset(model, generator ?? this.generator)
			);
			return result;
		}

		void Reset(
			BubblerModel model,
			Demon generator
		)
		{
			// Agent Properties
			// TODO: NavigationPlan and others may need to be reset...
			model.NavigationVelocity.Value = 40f; // How many meters per day they can walk...
			model.IsDebugging = false;
			model.NavigationForceDistanceMaximum.Value = 4f;
			model.Health.ResetToMaximum(30f);
			// model.Inventory.Reset(InventoryCapacity.ByTotalWeight(2));
			model.InteractionRadius.Value = 0.75f;
			
			// model.Clearable.Reset(
			// 	Inventory.FromEntry(Inventory.Types.Berries, 6),
			// 	maximumClearers: 3
			// );
			Debug.LogError("TODO: Clearable reset");
			
			model.LightSensitive.Reset();
		}
	}
}