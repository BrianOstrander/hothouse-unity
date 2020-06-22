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
			result.GlobalInventory = Inventory.Empty;
			result.GlobalInventoryCapacity = InventoryCapacity.None();
			result.LowRationThreshold = 0;
			result.Conditions = new Dictionary<Condition.Types, bool>().ToReadonlyDictionary();
			
			return result;
		}
		
		public TimeSpan LastUpdated { get; private set; }
		public int Population { get; private set; }
		public Inventory GlobalInventory { get; private set; }
		public InventoryCapacity GlobalInventoryCapacity { get; private set; }
		public int LowRationThreshold { get; private set; }
		public ReadOnlyDictionary<Condition.Types, bool> Conditions { get; private set; }

		public GameCache Calculate(GameModel game)
		{
			var result = new GameCache();

			result.LastUpdated = game.PlaytimeElapsed.Value;

			result.Population = game.Dwellers.AllActive.Length;

			var globalInventory = Inventory.Empty;
			var globalInventoryMaximumByIndividualWeight = Inventory.Empty;
			
			foreach (var building in game.Buildings.AllActive.Where(b => b.IsBuildingState(BuildingStates.Operating) && !b.Light.IsLight.Value))
			{
				globalInventory += building.Inventory.Value;
				globalInventoryMaximumByIndividualWeight += building.InventoryCapacity.Value.GetMaximum();
			}

			result.GlobalInventory = globalInventory;
			result.GlobalInventoryCapacity = InventoryCapacity.ByIndividualWeight(globalInventoryMaximumByIndividualWeight);
			
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