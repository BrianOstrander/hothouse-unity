using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Lunra.Core;

namespace Lunra.Hothouse.Models
{
	public struct GameCache
	{
		public TimeSpan LastUpdated { get; private set; }
		public Inventory GlobalInventory { get; private set; }
		public int LowRationThreshold { get; private set; }
		public ReadOnlyDictionary<Condition.Types, bool> Conditions { get; private set; }

		public GameCache Calculate(GameModel game)
		{
			var result = new GameCache();

			result.LastUpdated = game.PlaytimeElapsed.Value;
			
			result.GlobalInventory = game.Buildings.AllActive
				.Where(b => b.IsBuildingState(BuildingStates.Operating) && !b.Light.IsLight.Value)
				.Select(b => b.Inventory.Value)
				.Sum();

			result.LowRationThreshold = game.Dwellers.AllActive.Sum(d => d.LowRationThreshold.Value);
			
			result.Conditions = EnumExtensions
				.GetValues(Condition.Types.Unknown)
				.ToReadonlyDictionary(
					condition => condition,
					condition => Condition.Calculate(game, condition)
				);
			
			return result;
		}
	}
}