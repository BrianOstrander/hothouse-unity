using System;
using System.Linq;
using Lunra.Core;
using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Models.AgentModels;
using UnityEngine;

namespace Lunra.WildVacuum.Ai
{
	public class DwellerUnloadItemsState<S> : AgentState<GameModel, DwellerModel>
		where S : AgentState<GameModel, DwellerModel>
	{
		public override string Name => "UnloadItems";

		public struct Target
		{
			public readonly ItemCacheBuildingModel ItemCache;
			public readonly Inventory ItemsToUnload;

			public Target(
				ItemCacheBuildingModel itemCache,
				Inventory itemsToUnload
			)
			{
				ItemCache = itemCache;
				ItemsToUnload = itemsToUnload;
			}

			public Target NewItemsToUnload(Inventory itemsToUnload)
			{
				return new Target(
					ItemCache,
					itemsToUnload
				);
			}
		}

		Target target;
		float cooldownElapsed;

		public override void OnInitialize()
		{
			AddTransitions(
				new ToReturnOnAllItemsUnloaded(this),
				new ToReturnOnItemCacheAtCapacity(this)
			);
		}
		
		public void SetTarget(Target target) => this.target = target;

		public override void Idle(float delta)
		{
			cooldownElapsed += delta;

			if (cooldownElapsed < Agent.MeleeCooldown.Value) return;

			cooldownElapsed = cooldownElapsed % Agent.MeleeCooldown.Value;

			// Debug.LogWarning("TODO: item unloading here");
			var nextToUnload = target.ItemsToUnload.Current.First(
				i => 0 < i.Count && 0 < target.ItemCache.Inventory.Value.GetCapacity(i.Type)
			).Type;

			target.ItemCache.Inventory.Value = target.ItemCache.Inventory.Value.Add(1, nextToUnload);
			Agent.Inventory.Value = Agent.Inventory.Value.Subtract(1, nextToUnload);

			target = target.NewItemsToUnload(target.ItemsToUnload.Subtract(1, nextToUnload));
		}

		public override void End()
		{
			target = default;
			cooldownElapsed = 0f;
		}

		class ToReturnOnAllItemsUnloaded : AgentTransition<S, GameModel, DwellerModel>
		{
			DwellerUnloadItemsState<S> sourceState;

			public ToReturnOnAllItemsUnloaded(DwellerUnloadItemsState<S> sourceState) => this.sourceState = sourceState;
			
			public override bool IsTriggered() => sourceState.target.ItemsToUnload.IsEmpty;
		}
		
		class ToReturnOnItemCacheAtCapacity : AgentTransition<S, GameModel, DwellerModel>
		{
			DwellerUnloadItemsState<S> sourceState;

			public ToReturnOnItemCacheAtCapacity(DwellerUnloadItemsState<S> sourceState) => this.sourceState = sourceState;

			public override bool IsTriggered()
			{
				return sourceState.target.ItemsToUnload.Current
					.Where(i => 0 < i.Count)
					.None(i => 0 < sourceState.target.ItemCache.Inventory.Value.GetCapacity(i.Type));
			}
		}
	}
}