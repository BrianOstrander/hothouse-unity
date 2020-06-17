using System;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Models.AgentModels;
using UnityEngine;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class TransferItemsState<S> : AgentState<GameModel, DwellerModel>
		where S : AgentState<GameModel, DwellerModel>
	{
		public override string Name => "TransferItems";

		public struct Target
		{
			public readonly Action<Inventory> SetDestination;
			public readonly Func<Inventory> GetDestination;
			public readonly Func<Inventory.Types, int> GetDestinationCapacity;
			
			public readonly Action<Inventory> SetSource;
			public readonly Func<Inventory> GetSource;
			
			public readonly Inventory ItemsToTransfer;
			public readonly float TransferCooldown;

			public readonly Action Done;

			public Target(
				Action<Inventory> setDestination,
				Func<Inventory> getDestination,
				Func<Inventory.Types, int> getDestinationCapacity,
				Action<Inventory> setSource,
				Func<Inventory> getSource,
				Inventory itemsToTransfer,
				float transferCooldown,
				Action done = null
			)
			{
				SetDestination = setDestination;
				GetDestination = getDestination;
				GetDestinationCapacity = getDestinationCapacity;
				SetSource = setSource;
				GetSource = getSource;
				ItemsToTransfer = itemsToTransfer;
				TransferCooldown = transferCooldown;
				Done = done;
			}

			public Target NewItemsToUnload(Inventory itemsToUnload)
			{
				return new Target(
					SetDestination,
					GetDestination,
					GetDestinationCapacity,
					SetSource,
					GetSource,
					itemsToUnload,
					TransferCooldown,
					Done
				);
			}
		}

		Target target;
		float cooldownElapsed;

		public override void OnInitialize()
		{
			AddTransitions(
				new ToReturnOnAllItemsTransferred(),
				new ToReturnOnDestinationAtCapacity()
			);
		}
		
		public void SetTarget(Target target) => this.target = target;

		public override void Idle()
		{
			cooldownElapsed += Game.SimulationDelta;

			if (cooldownElapsed < target.TransferCooldown) return;

			cooldownElapsed = cooldownElapsed % target.TransferCooldown;

			(Inventory.Types Type, int Weight) itemToUnload = (Inventory.Types.Unknown, 0);

			foreach (var item in target.ItemsToTransfer.Entries)
			{
				if (item.Weight == 0) continue;
				if (target.GetSource()[item.Type] == 0) continue;
				if (target.GetDestinationCapacity(item.Type) <= 0) continue;
				itemToUnload = (item.Type, item.Weight);
				break;
			}
			
			if (itemToUnload.Type == Inventory.Types.Unknown)
			{
				target = target.NewItemsToUnload(Inventory.Empty);
				return;
			}

			// TODO: The amount transfered at a time should probably be defined somewhere and not hardcoded...
			itemToUnload.Weight = 1;

			target.SetDestination(target.GetDestination() + itemToUnload);
			target.SetSource(target.GetSource() - itemToUnload);
			
			target = target.NewItemsToUnload(target.ItemsToTransfer - itemToUnload);
		}

		public override void End()
		{
			var done = target.Done;
			target = default;
			done?.Invoke();
			cooldownElapsed = 0f;
		}

		class ToReturnOnAllItemsTransferred : AgentTransition<TransferItemsState<S>, S, GameModel, DwellerModel>
		{
			public override bool IsTriggered() => SourceState.target.ItemsToTransfer.IsEmpty;
		}
		
		class ToReturnOnDestinationAtCapacity : AgentTransition<TransferItemsState<S>, S, GameModel, DwellerModel>
		{
			public override bool IsTriggered()
			{
				return SourceState.target.ItemsToTransfer.Entries
					.Where(i => 0 < i.Weight)
					.None(i => 0 <= SourceState.target.GetDestinationCapacity(i.Type));
			}
		}
	}
}