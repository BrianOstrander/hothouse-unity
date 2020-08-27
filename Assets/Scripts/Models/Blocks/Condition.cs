using System;
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
			NoStalks = 101,
			NoScrap = 102,
			
			// Environment
			ZeroDoorsOpen = 200,
			AnyDoorsOpen = 201,
			AnyDoorsClosedAndLit = 202,
			
			// Flora
			SeenStalksFlora = 300,
			SeenEdibleFlora = 301,
			SeenAttackFlora = 302
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

		public static bool Calculate(
			GameModel game,
			Types type
		)
		{
			int doorOpenCount() => game.Doors.AllActive.Count(d => d.IsOpen.Value);

			bool getCachedTrueOrCalculate(Func<bool> calculate)
			{
				if (game.Cache.Value.Conditions.TryGetValue(type, out var value) && value) return true;
				return calculate();
			}
			
			switch (type)
			{
				// Constant
				case Types.ConstantFalse:
					return false;
				case Types.ConstantTrue:
					return true;
				
				// Building
				case Types.SingleOperationalFire:
					return game.Query.All<ILightModel>(m => m.Light.IsLightActive()).Count() == 1;
				case Types.AnyFireExtinguishing:
					return game.Query.Any<ILightModel>(m => m.Light.LightState.Value == LightStates.Extinguishing);
				case Types.ZeroBeds:
					return true;
					// return game.Buildings.AllActive.None(t => t.IsBuildingState(BuildingStates.Operating) && t.IsDesireAvailable(Motives.Sleep));
				
				// Inventory
				case Types.NoStalks: 
					throw new NotImplementedException("uhg");
					// return game.Cache.Value.GlobalInventory.All.Value[Inventory.Types.Stalk] <= 0;
				case Types.NoScrap: 
					throw new NotImplementedException("uhg2");
					// return game.Cache.Value.GlobalInventory.All.Value[Inventory.Types.Scrap] <= 0;

				// Environment
				case Types.ZeroDoorsOpen:
					return 0 == doorOpenCount();
				case Types.AnyDoorsOpen:
					return 0 < doorOpenCount();
				case Types.AnyDoorsClosedAndLit:
					return game.Doors.AllActive.Any(d => !d.IsOpen.Value && d.LightSensitive.IsLit);
				
				// Flora
				case Types.SeenStalksFlora:
					return getCachedTrueOrCalculate(
						() => true
						// () => game.Flora.AllActive.Any(m => m.Species.Value == FloraSpecies.Stalks && m.LightSensitive.IsLit)
					);
				case Types.SeenEdibleFlora:
					return getCachedTrueOrCalculate(
						() => true
						// () => game.Flora.AllActive.Any(m => m.Species.Value == FloraSpecies.Wheat && m.LightSensitive.IsLit)
					);
				case Types.SeenAttackFlora:
					return getCachedTrueOrCalculate(
						() => true
						// () => game.Flora.AllActive.Any(m => m.Species.Value == FloraSpecies.Shroom && m.LightSensitive.IsLit)
					);
					
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