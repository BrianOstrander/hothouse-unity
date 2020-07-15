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
				new BalanceItemState.ToBalanceOnAvailableDistribution(),
				
				new CleanupState.ToCleanupOnItemsAvailable()
			);
		}

		public override void Idle()
		{
			if (cache.LastUpdated < Game.NavigationMesh.LastUpdated.Value) cache = new Cache();
		}
	}
}