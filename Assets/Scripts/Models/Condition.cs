using System.Linq;
using Lunra.Core;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public struct Condition
	{
		public enum Types
		{
			Unknown = 0,
			
			// Building
			SingleOperationalFire = 1,
			AnyFireExtinguishing = 2,
			ZeroBeds = 3,
			
			// Inventory
			NoRations = 100,
			NoStalks = 101,
			NoScrap = 102,
			LowRations = 103,
			
			// Environment
			ZeroOpenDoors = 200
		}

		public readonly bool DefaultValue;
		public readonly Types[] Any;
		public readonly Types[] All;
		public readonly Types[] None;

		public Condition(
			bool defaultValue,
			Types[] any,
			Types[] all,
			Types[] none	
		)
		{
			DefaultValue = defaultValue;
			Any = any ?? new Types[0];
			All = all ?? new Types[0];
			None = none ?? new Types[0];
		}

		public bool Evaluate(GameCache gameCache)
		{
			foreach (var current in All)
			{
				if (!gameCache.Conditions[current]) return false;
			}
			
			foreach (var current in None)
			{
				if (gameCache.Conditions[current]) return false;
			}
			
			foreach (var current in Any)
			{
				if (gameCache.Conditions[current]) return true;
			}

			return DefaultValue;
		}
		
		public bool Evaluate(GameModel game)
		{
			foreach (var current in All)
			{
				if (!Calculate(game, current)) return false;
			}
			
			foreach (var current in None)
			{
				if (Calculate(game, current)) return false;
			}
			
			foreach (var current in Any)
			{
				if (Calculate(game, current)) return true;
			}

			return DefaultValue;
		}

		public static bool Calculate(GameModel game, Types type)
		{
			switch (type)
			{
				// Building
				case Types.SingleOperationalFire:
					return game.Lights.Count() == 1;
				case Types.AnyFireExtinguishing:
					return game.Lights.Any(l => l.LightState.Value == LightStates.Extinguishing);
				case Types.ZeroBeds:
					return game.Buildings.AllActive.None(t => t.IsDesireAvailable(Desires.Sleep));
				
				// Inventory
				case Types.NoRations:
					return game.Cache.Value.GlobalInventory[Inventory.Types.Rations] <= 0;
				case Types.NoStalks: 
					return game.Cache.Value.GlobalInventory[Inventory.Types.Rations] <= 0;
				case Types.NoScrap: 
					return game.Cache.Value.GlobalInventory[Inventory.Types.Rations] <= 0;
				case Types.LowRations:
					return game.Cache.Value.GlobalInventory[Inventory.Types.Rations] <= game.Cache.Value.LowRationThreshold;
				
				// Environment
				case Types.ZeroOpenDoors:
					return game.Doors.AllActive.None(d => d.IsOpen.Value);
				
				// Invalid or Unrecognized
				case Types.Unknown:
					Debug.LogError("Invalid "+nameof(type)+": "+type);
					break;
				default:
					Debug.LogError("Unrecognized "+nameof(type)+": "+type);
					break;
			}

			return false;
		}
	}
}