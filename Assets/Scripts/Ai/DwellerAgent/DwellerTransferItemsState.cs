using System;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Models.AgentModels;
using UnityEngine;

namespace Lunra.Hothouse.Ai
{
	public class DwellerTransferItemsState<S> : AgentState<GameModel, DwellerModel>
		where S : AgentState<GameModel, DwellerModel>
	{
		public override string Name => "TransferItems";

		public struct Target
		{
			public readonly Action<Inventory> SetDestination;
			public readonly Func<Inventory> GetDestination;
			public readonly Func<(Item.Types Type, int Weight), int> GetDestinationCapacity;
			
			public readonly Action<Inventory> SetSource;
			public readonly Func<Inventory> GetSource;
			
			public readonly Inventory ItemsToTransfer;
			public readonly float TransferCooldown;

			public Target(
				Action<Inventory> setDestination,
				Func<Inventory> getDestination,
				Func<(Item.Types Type, int Weight), int> getDestinationCapacity,
				Action<Inventory> setSource,
				Func<Inventory> getSource,
				Inventory itemsToTransfer,
				float transferCooldown
			)
			{
				SetDestination = setDestination;
				GetDestination = getDestination;
				GetDestinationCapacity = getDestinationCapacity;
				SetSource = setSource;
				GetSource = getSource;
				ItemsToTransfer = itemsToTransfer;
				TransferCooldown = transferCooldown;
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
					TransferCooldown
				);
			}
		}

		Target target;
		float cooldownElapsed;

		public override void OnInitialize()
		{
			AddTransitions(
				new ToReturnOnAllItemsTransfered(this),
				new ToReturnOnDestinationAtCapacity(this)
			);
		}
		
		public void SetTarget(Target target) => this.target = target;

		public override void Idle()
		{
			cooldownElapsed += World.SimulationDelta;

			if (cooldownElapsed < target.TransferCooldown) return;

			cooldownElapsed = cooldownElapsed % target.TransferCooldown;

			(Item.Types Type, int Weight) itemToUnload = (Item.Types.Unknown, 0);

			foreach (var item in target.ItemsToTransfer.Entries)
			{
				if (item.Value == 0) continue;
				if (target.GetSource()[item.Key] == 0) continue;
				if (target.GetDestinationCapacity((item.Key, item.Value)) <= 0) continue;
				itemToUnload = (item.Key, item.Value);
				break;
			}
			
			if (itemToUnload.Type == Item.Types.Unknown)
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
			target = default;
			cooldownElapsed = 0f;
		}

		class ToReturnOnAllItemsTransfered : AgentTransition<S, GameModel, DwellerModel>
		{
			DwellerTransferItemsState<S> sourceState;

			public ToReturnOnAllItemsTransfered(DwellerTransferItemsState<S> sourceState) => this.sourceState = sourceState;
			
			public override bool IsTriggered() => sourceState.target.ItemsToTransfer.IsEmpty;
		}
		
		class ToReturnOnDestinationAtCapacity : AgentTransition<S, GameModel, DwellerModel>
		{
			DwellerTransferItemsState<S> sourceState;

			public ToReturnOnDestinationAtCapacity(DwellerTransferItemsState<S> sourceState) => this.sourceState = sourceState;

			public override bool IsTriggered()
			{
				return sourceState.target.ItemsToTransfer.Entries
					.Where(i => 0 < i.Value)
					.None(i => 0 <= sourceState.target.GetDestinationCapacity((i.Key, i.Value)));
			}
		}
	}
}