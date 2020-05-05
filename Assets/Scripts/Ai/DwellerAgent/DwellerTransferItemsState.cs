using System;
using System.Linq;
using Lunra.Core;
using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Models.AgentModels;

namespace Lunra.WildVacuum.Ai
{
	public class DwellerTransferItemsState<S> : AgentState<GameModel, DwellerModel>
		where S : AgentState<GameModel, DwellerModel>
	{
		public override string Name => "TransferItems";

		public struct Target
		{
			public readonly Action<Inventory> SetDestination;
			public readonly Func<Inventory> GetDestination;
			
			public readonly Action<Inventory> SetSource;
			public readonly Func<Inventory> GetSource;
			
			public readonly Inventory ItemsToUnload;
			public readonly float TransferCooldown;

			public Target(
				Action<Inventory> setDestination,
				Func<Inventory> getDestination,
				Action<Inventory> setSource,
				Func<Inventory> getSource,
				Inventory itemsToUnload,
				float transferCooldown
			)
			{
				SetDestination = setDestination;
				GetDestination = getDestination;
				SetSource = setSource;
				GetSource = getSource;
				ItemsToUnload = itemsToUnload;
				TransferCooldown = transferCooldown;
			}

			public Target NewItemsToUnload(Inventory itemsToUnload)
			{
				return new Target(
					SetDestination,
					GetDestination,
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

		public override void Idle(float delta)
		{
			cooldownElapsed += delta;

			if (cooldownElapsed < target.TransferCooldown) return;

			cooldownElapsed = cooldownElapsed % target.TransferCooldown;

			var nextToUnload = target.ItemsToUnload.Current.First(
				i => 0 < i.Count && 0 < target.GetDestination().GetCapacity(i.Type)
			).Type;

			target.SetDestination(target.GetDestination().Add(1, nextToUnload));
			target.SetSource(target.GetSource().Subtract(1, nextToUnload));
			
			target = target.NewItemsToUnload(target.ItemsToUnload.Subtract(1, nextToUnload));
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
			
			public override bool IsTriggered() => sourceState.target.ItemsToUnload.IsEmpty;
		}
		
		class ToReturnOnDestinationAtCapacity : AgentTransition<S, GameModel, DwellerModel>
		{
			DwellerTransferItemsState<S> sourceState;

			public ToReturnOnDestinationAtCapacity(DwellerTransferItemsState<S> sourceState) => this.sourceState = sourceState;

			public override bool IsTriggered()
			{
				return sourceState.target.ItemsToUnload.Current
					.Where(i => 0 < i.Count)
					.None(i => 0 < sourceState.target.GetDestination().GetCapacity(i.Type));
			}
		}
	}
}