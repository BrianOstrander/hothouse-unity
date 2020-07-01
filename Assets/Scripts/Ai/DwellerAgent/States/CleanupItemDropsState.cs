using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Ai.Dweller;
using Lunra.Hothouse.Models;
using UnityEngine;

namespace Lunra.Hothouse.Ai
{
	public class CleanupItemDropsState<S0, S1> : AgentState<GameModel, DwellerModel>
		where S0 : AgentState<GameModel, DwellerModel>
		where S1 : CleanupItemDropsState<S0, S1>
	{
		public override string Name => "CleanupItemDrops";
		
		public override void OnInitialize()
		{
			AddChildStates(
				new InventoryRequestState<S1>()	
			);
		
			AddTransitions(
				new InventoryRequestState<S1>.ToInventoryRequestOnPromises(),
				new ToReturnOnFallthrough()
			);
		}

		public class ToCleanupOnItemsAvailable : AgentTransition<S0, S1, GameModel, DwellerModel>
		{
			class NavigationCache
			{
				public BuildingModel Model;
				public Vector3 Position;
				public bool IsNavigable;
				public Inventory AvailableCapacity;
				public Navigation.Result NavigationResult;
			}
			
			List<ItemDropModel> itemDrops = new List<ItemDropModel>();
			List<NavigationCache> navigationCache = new List<NavigationCache>();

			ItemDropModel selectedPickup;
			// BuildingModel selectedDropoff;
			NavigationCache selectedDropoffCache;
			
			public override bool IsTriggered()
			{
				if (!Game.Cache.Value.AnyItemDropsAvailableForPickup) return false;
				
				itemDrops.Clear();
				navigationCache.Clear();

				selectedPickup = null;
				// selectedDropoff = null;

				foreach (var itemDrop in Game.ItemDrops.AllActive)
				{
					if (!itemDrop.Enterable.AnyAvailable()) continue;
					if (itemDrop.Inventory.Available.Value.IsEmpty) continue;
					if (!itemDrop.Inventory.Permission.Value.CanWithdrawal(Agent)) continue;
					
					itemDrops.Add(itemDrop);
				}

				if (itemDrops.None()) return false;

				selectedDropoffCache = null;
				var buildingsRemaining = new Stack<BuildingModel>(Game.Buildings.AllActive);
				
				foreach (var itemDrop in itemDrops.OrderBy(m => Vector3.Distance(Agent.Transform.Position.Value, m.Transform.Position.Value)))
				{
					selectedPickup = itemDrop;
					
					var isItemDropNavigable = NavigationUtility.CalculateNearest(
						Agent.Transform.Position.Value,
						out var itemDropNavigationResult,
						Navigation.QueryEntrances(itemDrop)
					);
					
					if (!isItemDropNavigable) continue;

					selectedDropoffCache = navigationCache
						.Where(m => m.IsNavigable)
						.OrderBy(m => Vector3.Distance(itemDrop.Transform.Position.Value, m.Position))
						.FirstOrDefault(m => m.AvailableCapacity.Intersects(itemDrop.Inventory.Available.Value));

					if (selectedDropoffCache != null) break;
					if (buildingsRemaining.None()) return false;

					while (buildingsRemaining.Any())
					{
						var building = buildingsRemaining.Pop();

						if (CalculateBuilding(building, out selectedDropoffCache)) break;
						
						navigationCache.Add(selectedDropoffCache);
						selectedDropoffCache = null;
					}

					if (selectedDropoffCache != null) break;
				}

				if (selectedDropoffCache == null) return false;

				// selectedDropoff = selectedDropoffCache.Model;
				
				return true;
			}

			public override void Transition()
			{
				selectedPickup.Inventory.Available.Value.Intersects(
					selectedDropoffCache.AvailableCapacity,
					out var intersection
				);

				var carryingCapacity = Agent.Inventory.AllCapacity.Value.GetCapacityFor(Agent.Inventory.All.Value);

				carryingCapacity.Intersects(
					intersection,
					out intersection
				);

				/*
				var isValidDeliver = selectedDropoff.Inventory.RequestDeliver(
					intersection,
					out var deliverTransaction,
					out _
				);
				
				if (!isValidDeliver)
				{
					Debug.LogError("Deliver request failed, this is unexpected");
					return;
				}
				*/
				
				var isValidDistribute = selectedPickup.Inventory.RequestDistribution(
					intersection,
					out var distributeTransaction,
					out _
				);

				if (!isValidDistribute)
				{
					Debug.LogError("Distribute request failed, this is unexpected");
					return;
				}
				
				// Agent.InventoryPromises.Transactions.Push(deliverTransaction);
				Agent.InventoryPromises.Transactions.Push(distributeTransaction);
			}

			bool CalculateBuilding(
				BuildingModel model,
				out NavigationCache result
			)
			{
				result = new NavigationCache();
				result.Model = model;

				if (!model.Inventory.Permission.Value.CanDeposit(Agent)) return false;
				result.AvailableCapacity = model.Inventory.AvailableCapacity.Value.GetCapacityFor(model.Inventory.Available.Value);
				if (result.AvailableCapacity.IsEmpty) return false;
				
				result.IsNavigable = NavigationUtility.CalculateNearest(
					Agent.Transform.Position.Value,
					out result.NavigationResult,
					Navigation.QueryEntrances(model)
				);

				if (!result.IsNavigable) return false;

				result.Position = model.Transform.Position.Value;

				return true;
			}
		}

		class ToReturnOnFallthrough : AgentTransition<S1, S0, GameModel, DwellerModel>
		{
			public override bool IsTriggered() => true;
		}		
	}
}