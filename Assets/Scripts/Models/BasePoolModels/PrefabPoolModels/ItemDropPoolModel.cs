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
		public override void Initialize(GameModel game)
		{
			Initialize(
				game,
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
			// TODO: Probably need to move this to component initialize...
			if (IsInitialized) Game.LastLightUpdate.Value = Game.LastLightUpdate.Value.SetSensitiveStale(result.Id.Value);
			return result;
		}
		
		void Reset(
			ItemDropModel model,
			params Stack[] inventory
		)
		{
			model.Enterable.Reset();

			model.Inventory.Container.Deposit(
				Game.Items.Builder
					.BeginItem()
					.WithProperties(
						Items.Instantiate.Capacity.OfZero(
							IdCounter.UndefinedId
						)
					)
					.Done(out var capacityPool)
			);

			model.Inventory.Container.Deposit(
				Game.Items.Builder
					.BeginItem()
					.WithProperties(
						Items.Instantiate.Capacity.OfZero(
							capacityPool.Id
						)
					)
					.Done(out var capacity)
			);
			
			model.Inventory.Capacities.Add(
				capacity.Id,
				PropertyFilter.Default()
			);
			
			foreach (var element in Game.Items.InStack(inventory))
			{
				if (element.Item.TryGet(Items.Keys.Shared.Type, out var type))
				{
					if (type == Items.Values.Shared.Types.Resource)
					{
						element.Item[Items.Keys.Resource.LogisticState] = Items.Values.Resource.LogisticStates.None;
						element.Item[Items.Keys.Resource.ParentPool] = capacityPool.Id;
						model.Inventory.Container.Deposit(element.Stack);
					}
					else Debug.LogError($"Unrecognized type \"{type}\", was this inventory properly sanitized before dropping?");
				}
				else Debug.LogError($"Unable to get the {Items.Keys.Shared.Type} for {element.Item}");
			}
			
			model.Inventory.Calculate();

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