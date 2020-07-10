using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Hothouse.Models;
using UnityEngine;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class StockpilerState<S> : JobState<S, StockpilerState<S>>
		where S : AgentState<GameModel, DwellerModel>
	{
		struct NavigationEntry
		{
			public bool IsNavigable;
			public BuildingModel Model;
		}
		
		class Cache
		{
			public DateTime LastUpdated;
			public Dictionary<string, NavigationEntry> NavigationResults = new Dictionary<string, NavigationEntry>();

			public Cache()
			{
				LastUpdated = DateTime.Now;
			}
		}
		
		static readonly Buildings[] StockpilerWorkplaces = 
		{
			Buildings.StartingWagon,
			Buildings.DepotSmall
		};
		
		protected override Jobs Job => Jobs.Stockpiler;

		protected override Buildings[] Workplaces => StockpilerWorkplaces;

		Cache cache = new Cache();
		BuildingModel workplace;
		
		public override void OnInitialize()
		{
			AddChildStates(
				new CleanupState(),
				new InventoryRequestState(),
				new NavigateState()
			);

			AddTransitions(
				new ToReturnOnJobChanged(),
				new ToReturnOnShiftEnd(),
				new ToReturnOnWorkplaceMissing(),
				new ToReturnOnWorkplaceIsNotNavigable(),

				new InventoryRequestState.ToInventoryRequestOnPromises(),
				
				new CleanupState.ToCleanupOnItemsAvailable(),
				
				new ToNavigateToWorkplace(),
				
				new ToGatherForConstruction()
			);
		}

		public override void Begin()
		{
			if (!Agent.Workplace.Value.TryGetInstance(Game, out workplace))
			{
				Debug.LogError("In stockpile state but unable to find workplace, this is an invalid state");
			}
		}

		public override void Idle()
		{
			if (cache.LastUpdated < Game.NavigationMesh.LastUpdated.Value) cache = new Cache();
		}

		class ToGatherForConstruction : AgentTransition<StockpilerState<S>, InventoryRequestState, GameModel, DwellerModel>
		{
			InventoryComponent target;
			Inventory inventoryForDelivery;

			public override bool IsTriggered()
			{
				foreach (var model in Game.Buildings.AllActive)
				{
					if (!model.IsBuildingState(BuildingStates.Constructing)) continue;
					if (!model.Enterable.AnyAvailable()) continue;
					
					var existingEntryExists = SourceState.cache.NavigationResults.TryGetValue(model.Id.Value, out var existingEntry);

					if (!existingEntryExists)
					{
						existingEntry = new NavigationEntry();
						existingEntry.Model = model;

						if (Navigation.TryQuery(model, out var query))
						{
							existingEntry.IsNavigable = NavigationUtility.CalculateNearest(
								Agent.Transform.Position.Value,
								out _,
								query
							);
						}
					}
					
					if (!existingEntry.IsNavigable) continue;
					
					var isIntersecting = existingEntry.Model.ConstructionInventory.AvailableCapacity.Value.GetMaximum().Intersects(
						SourceState.workplace.Inventory.Available.Value,
						out var intersection
					);

					if (isIntersecting)
					{
						target = model.ConstructionInventory;
						inventoryForDelivery = Inventory.FromEntries(intersection.Entries.First(i => 0 < i.Weight));
						return true;
					}
				}

				return false;
			}

			public override void Transition()
			{
				target.RequestDeliver(
					inventoryForDelivery,
					out var deliveryTransaction,
					out _
				);
				Agent.InventoryPromises.Transactions.Push(deliveryTransaction);
			}
		}
	}
}