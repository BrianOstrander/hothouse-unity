using System;
using System.Collections.Generic;
using Lunra.Core;
using Lunra.Hothouse.Presenters;
using Lunra.Hothouse.Views;
using Lunra.NumberDemon;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class ItemDropPoolModel : BasePrefabPoolModel<ItemDropModel>
	{
		GameModel game;
		
		public override void Initialize(GameModel game)
		{
			this.game = game;
			Initialize(
				model => new ItemDropPresenter(game, model)	
			);
		}

		public ItemDropModel Activate(
			string roomId,
			Vector3 position,
			Quaternion rotation,
			Inventory inventory
		)
		{
			var result = Activate(
				"default",
				roomId,
				position,
				rotation,
				model => Reset(model, inventory)
			);
			if (IsInitialized) game.LastLightUpdate.Value = game.LastLightUpdate.Value.SetSensitiveStale(result.Id.Value);
			return result;
		}
		
		void Reset(
			ItemDropModel model,
			Inventory inventory
		)
		{
			model.Enterable.Reset();
			model.Inventory.Reset(
				InventoryPermission.WithdrawalForJobs(EnumExtensions.GetValues(Jobs.Unknown)),
				InventoryCapacity.ByIndividualWeight(inventory)
			);
			model.Inventory.Add(inventory);
		}
	}
}