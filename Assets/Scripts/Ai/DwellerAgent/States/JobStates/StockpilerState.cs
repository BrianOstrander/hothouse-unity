using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Hothouse.Models;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class StockpilerState<S> : JobState<S, StockpilerState<S>>
		where S : AgentState<GameModel, DwellerModel>
	{
		struct NavigationEntry
		{
			public bool IsNavigable;
			public bool IsFull;
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
		
		static readonly BuildingTypes[] StockpilerWorkplaces = 
		{
			BuildingTypes.Stockpile
		};
		
		protected override Jobs Job => Jobs.Stockpiler;

		protected override BuildingTypes[] Workplaces => StockpilerWorkplaces;

		Cache cache = new Cache();
		
		public override void OnInitialize()
		{
			AddChildStates(
				new CleanupState(),
				new InventoryRequestState(),
				new NavigateState(),
				new BalanceItemState()
			);

			AddTransitions(
				new ToReturnOnJobChanged(),
				new ToReturnOnShiftEnd(),
				new ToReturnOnWorkplaceMissing(),
				new ToReturnOnWorkplaceIsNotNavigable(),

				new InventoryRequestState.ToInventoryRequestOnPromises(),
				
				new ToNavigateToWorkplace(),
				
				new BalanceItemState.ToBalanceOnAvailableDelivery(),
				new ToGatherForConstruction(),
				
				new BalanceItemState.ToBalanceOnAvailableDistribution(),
				new CleanupState.ToCleanupOnItemsAvailable()
			);
		}

		public override void Idle()
		{
			if (cache.LastUpdated < Game.NavigationMesh.LastUpdated.Value) cache = new Cache();
		}

		bool IsCurrentlyAtWorkplace()
		{
			return TryCalculateWorkplaceNavigation(out var isCurrentlyAtWorkplace, out _) && isCurrentlyAtWorkplace;
		}
		
		class ToGatherForConstruction : AgentTransition<StockpilerState<S>, InventoryRequestState, GameModel, DwellerModel>
		{
			InventoryComponent target;
			Inventory itemsForConstruction;

			public override bool IsTriggered()
			{
				if (!SourceState.IsCurrentlyAtWorkplace()) return false;
				
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
						
						SourceState.cache.NavigationResults.Add(model.Id.Value, existingEntry);
					}
					
					if (!existingEntry.IsNavigable) continue;
					if (existingEntry.IsFull || (existingEntry.IsFull = model.ConstructionInventory.IsFull())) continue;
					
					var isIntersecting = model.ConstructionInventory.AvailableCapacity.Value.GetCapacityFor(model.ConstructionInventory.Available.Value).Intersects(
						SourceState.Workplace.Inventory.Available.Value,
						out var intersection
					);

					if (isIntersecting)
					{
						isIntersecting = Agent.Inventory.AllCapacity.Value.GetMaximum().Intersects(
							intersection,
							out intersection
						);
						
						if (isIntersecting)
						{
							target = model.ConstructionInventory;
							itemsForConstruction = Inventory.FromEntries(intersection.Entries.First(i => 0 < i.Weight));
							return true;
						}
					}
				}

				return false;
			}

			public override void Transition()
			{
				Agent.InventoryPromises.Push(
					itemsForConstruction,
					SourceState.Workplace.Inventory,
					target
				);
			}
		}
	}
}