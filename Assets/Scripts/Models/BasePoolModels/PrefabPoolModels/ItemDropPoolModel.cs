using System.Collections.Generic;
using System.Linq;
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

			game.Items.Iterate(
				(item, stack) =>
				{
					if (item.TryGet(Items.Keys.Shared.Type, out var type))
					{
						if (type == Items.Values.Shared.Types.Resource)
						{
							item.Set(Items.Keys.Resource.Logistics.State, Items.Values.Resource.Logistics.States.Distribute);

							model.Inventory.Container.Add(
								stack,
								model.Inventory.Container
									.Build()
									.WithProperties(
										Items.Instantiate.Capacity.OfZero(item.Get(Items.Keys.Resource.Id))
									)
							);
						}
						else Debug.LogError($"Unrecognized type \"{type}\", was this inventory properly sanitized before dropping?");
					}
					else Debug.LogError($"Unable to get the {Items.Keys.Shared.Type} for {item}");
				},
				inventory
			);
			
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