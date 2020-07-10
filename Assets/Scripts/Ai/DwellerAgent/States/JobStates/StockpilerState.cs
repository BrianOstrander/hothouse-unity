using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Hothouse.Models;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class StockpilerState<S> : JobState<S, StockpilerState<S>>
		where S : AgentState<GameModel, DwellerModel>
	{
		class Cache
		{
			public DateTime LastUpdated;
			public Dictionary<string, bool> NavigationResults = new Dictionary<string, bool>();

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
				
				new ToNavigateToWorkplace()
			);
		}

		public override void Begin()
		{
			if (cache.LastUpdated < Game.NavigationMesh.LastUpdated.Value) cache = new Cache();
		}

		public override void Idle()
		{
			
			// var possibleConstructionSites = Game.Buildings.AllActive
			// 	.Where(m => m.IsBuildingState(BuildingStates.Constructing))
			// 	.Where(m => m.ConstructionInventory.IsNotFull())
			// 	.Where(m => m.ConstructionInventory.AvailableCapacity.Value.GetMaximum().Intersects())
		}
	}
}