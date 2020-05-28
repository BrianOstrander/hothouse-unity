using System.Diagnostics.Contracts;
using System.Linq;
using Lunra.Core;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public struct Condition
	{
		public static Condition New(
			Types[] any = null,
			Types[] all = null,
			Types[] none = null
		)
		{
			return new Condition(
				any,
				all,
				none
			);
		}
		
		public static Condition Any(params Types[] types)
		{
			return new Condition(
				any: types
			);
		}
		
		public static Condition All(params Types[] types)
		{
			return new Condition(
				all: types
			);
		}
		
		public static Condition None(params Types[] types)
		{
			return new Condition(
				none: types
			);
		}
		
		public enum Types
		{
			// Constant
			ConstantFalse = -2,
			ConstantTrue = -1,
			
			// Default
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

		[JsonProperty] readonly Types[] any;
		[JsonProperty] readonly Types[] all;
		[JsonProperty] readonly Types[] none;

		public Condition(
			Types[] any = null,
			Types[] all = null,
			Types[] none = null
		)
		{
			this.any = any ?? new Types[0];
			this.all = all ?? new Types[0];
			this.none = none ?? new Types[0];
		}

		[Pure]
		public bool Evaluate(GameCache gameCache)
		{
			foreach (var current in all)
			{
				if (!gameCache.Conditions[current]) return false;
			}
			
			foreach (var current in none)
			{
				if (gameCache.Conditions[current]) return false;
			}
			
			foreach (var current in any)
			{
				if (gameCache.Conditions[current]) return true;
			}

			return true;
		}
		
		[Pure]
		public bool Evaluate(GameModel game)
		{
			foreach (var current in all)
			{
				if (!Calculate(game, current)) return false;
			}
			
			foreach (var current in none)
			{
				if (Calculate(game, current)) return false;
			}
			
			foreach (var current in any)
			{
				if (Calculate(game, current)) return true;
			}

			return true;
		}

		public static bool Calculate(GameModel game, Types type)
		{
			switch (type)
			{
				// Constant
				case Types.ConstantFalse:
					return false;
				case Types.ConstantTrue:
					return true;
				
				// Building
				case Types.SingleOperationalFire:
					return game.GetLightsActive().Count() == 1;
				case Types.AnyFireExtinguishing:
					return game.GetLightsActive().Any(l => l.Light.LightState.Value == LightStates.Extinguishing);
				case Types.ZeroBeds:
					return game.Buildings.AllActive.None(t => t.IsBuildingState(BuildingStates.Operating) && t.IsDesireAvailable(Desires.Sleep));
				
				// Inventory
				case Types.NoRations:
					return game.Cache.Value.GlobalInventory[Inventory.Types.Rations] <= 0;
				case Types.NoStalks: 
					return game.Cache.Value.GlobalInventory[Inventory.Types.Stalks] <= 0;
				case Types.NoScrap: 
					return game.Cache.Value.GlobalInventory[Inventory.Types.Scrap] <= 0;
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