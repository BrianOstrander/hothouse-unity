using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Lunra.Core;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public struct GameCache
	{
		public static GameCache Default()
		{
			var result = new GameCache();
			
			result.LastUpdated = TimeSpan.Zero;
			result.Population = 0;
			result.GlobalItemDropsAvailable = Inventory.Empty;
			result.GlobalInventory = new InventoryComponent();
			result.GlobalInventory.Reset(InventoryPermission.AllForAnyJob(), InventoryCapacity.None());
			result.AnyItemDropsAvailableForPickup = false;
			result.UniqueObligationsAvailable = new string[0];
			result.AnyObligationsAvailable = false;
			result.LowRationThreshold = 0;
			result.Conditions = new Dictionary<Condition.Types, bool>().ToReadonlyDictionary();
			
			return result;
		}
		
		public TimeSpan LastUpdated { get; private set; }
		public int Population { get; private set; }
		
		public InventoryComponent GlobalInventory { get; private set; }
		public Inventory GlobalItemDropsAvailable { get; private set; }
		public bool AnyItemDropsAvailableForPickup { get; private set; }
		public string[] UniqueObligationsAvailable { get; private set; }
		public bool AnyObligationsAvailable { get; private set; }
		public int LowRationThreshold { get; private set; }
		public ReadOnlyDictionary<Condition.Types, bool> Conditions { get; private set; }

		public GameCache Calculate(GameModel game)
		{
			var result = Default();

			result.LastUpdated = game.PlaytimeElapsed.Value;

			result.Population = game.Dwellers.AllActive.Length;

			var globalInventoryAll = Inventory.Empty;
			var globalInventoryAllCapacity = Inventory.Empty;
			var globalInventoryForbidden = Inventory.Empty;
			var globalInventoryReserved = Inventory.Empty;
			
			foreach (var model in game.Buildings.AllActive.Where(b => b.IsBuildingState(BuildingStates.Operating) && b.Enterable.AnyAvailable()))
			{
				globalInventoryAll += model.Inventory.All.Value;
				globalInventoryAllCapacity += model.Inventory.AllCapacity.Value.GetMaximum();
				globalInventoryForbidden += model.Inventory.Forbidden.Value;
				globalInventoryReserved += model.Inventory.ReservedCapacity.Value.GetMaximum();
			}

			result.GlobalInventory.Reset(
				InventoryPermission.AllForAnyJob(),
				InventoryCapacity.ByIndividualWeight(globalInventoryAllCapacity)
			);
			result.GlobalInventory.Add(globalInventoryAll);
			result.GlobalInventory.AddForbidden(globalInventoryForbidden);
			result.GlobalInventory.AddReserved(globalInventoryReserved);

			foreach (var model in game.ItemDrops.AllActive)
			{
				result.GlobalItemDropsAvailable += model.Inventory.Available.Value;
			}

			if (!result.GlobalItemDropsAvailable.IsEmpty)
			{
				result.AnyItemDropsAvailableForPickup = result.GlobalInventory.AvailableCapacity.Value
					.HasCapacityFor(result.GlobalInventory.Available.Value, result.GlobalItemDropsAvailable);
			}

			result.UniqueObligationsAvailable = game.GetObligations()
				.SelectMany(m => m.Obligations.All.Value.Available)
				.Select(o => o.Type)
				.Distinct()
				.ToArray();

			result.AnyObligationsAvailable = UniqueObligationsAvailable.Any();
			
			result.LowRationThreshold = game.Dwellers.AllActive.Sum(d => d.LowRationThreshold.Value);

			result.Conditions = EnumExtensions
				.GetValues(Condition.Types.Unknown)
				.ToReadonlyDictionary(
					condition => condition,
					condition =>
					{
						// TODO: Check certain one-time only conditions here... 
						return Condition.Calculate(game, condition);
					}
				);
			
			return result;
		}
	}
}