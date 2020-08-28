using Lunra.Core;
using Lunra.Hothouse.Presenters;
using Lunra.Satchel;
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
			IRoomTransformModel origin,
			Quaternion rotation,
			params Stack[] inventory
		)
		{
			return Activate(
				origin.RoomTransform.Id.Value,
				origin.Transform.Position.Value,
				rotation,
				inventory
			);
		}

		public ItemDropModel Activate(
			string roomId,
			Vector3 position,
			Quaternion rotation,
			params Stack[] inventory
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
			params Stack[] inventory
		)
		{
			model.Enterable.Reset();
			model.Inventory.Reset(game.Items);

			model.Inventory.Container
				.Add(inventory);
			// model.Inventory.Reset(
			// 	InventoryPermission.WithdrawalForJobs(EnumExtensions.GetValues(Jobs.Unknown)),
			// 	InventoryCapacity.ByIndividualWeight(inventory)
			// );
			// model.Inventory.Add(inventory);
			// model.Inventory.Desired.Value = InventoryDesire.UnCalculated(Inventory.Empty);
			Debug.LogWarning("TODO: Handle Inventory");
		}
	}
}