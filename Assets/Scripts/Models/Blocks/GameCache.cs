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
			result.GoalsDiscontentDeltaByMotive = EnumExtensions.GetValues(Motives.Unknown, Motives.None).Select(m => (m, 0f)).ToArray();
			result.LastPopulationDecrease = DayTime.Zero;
			result.LastPopulationIncrease = DayTime.Zero;
			result.LastPopulationChange = DayTime.Zero;
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
		public GoalSnapshot GoalsAverage { get; private set; }
		public float GoalsDiscontentDelta { get; private set; }
		public (Motives Motive, float DiscontentDelta)[] GoalsDiscontentDeltaByMotive { get; private set; }
		public DayTime LastPopulationDecrease { get; private set; }
		public DayTime LastPopulationIncrease { get; private set; }
		public DayTime LastPopulationChange { get; private set; }
		
		public ReadOnlyDictionary<Condition.Types, bool> Conditions { get; private set; }

		public GameCache Calculate(GameModel game)
		{
			var result = Default();

			result.LastUpdated = game.PlaytimeElapsed.Value;

			var globalInventoryAll = Inventory.Empty;
			var globalInventoryAllCapacity = Inventory.Empty;
			var globalInventoryForbidden = Inventory.Empty;
			var globalInventoryReserved = Inventory.Empty;
			
			foreach (var model in game.Buildings.AllActive.Where(b => b.IsBuildingState(BuildingStates.Operating) && b.Tags.Contains(BuildingTags.Stockpile)))
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

			GoalResult getGoalResult((float Insistence, float Discontent, float DiscontentRangeMinimum, float DiscontentRangeMaximum) value)
			{
				return new GoalResult(
					value.Insistence,
					value.Discontent,
					new FloatRange(value.DiscontentRangeMinimum, value.DiscontentRangeMaximum)
				);
			}
			
			(float Insistence, float Discontent, float DiscontentRangeMinimum, float DiscontentRangeMaximum) goalsTotal = default;
			var goals = new Dictionary<Motives, (float Insistence, float Discontent, float DiscontentRangeMinimum, float DiscontentRangeMaximum)>();

			foreach (var motive in EnumExtensions.GetValues(Motives.Unknown, Motives.None)) goals.Add(motive, default);

			foreach (var dweller in game.Dwellers.AllActive)
			{
				result.LowRationThreshold += dweller.LowRationThreshold.Value;
				result.Population++;
				
				goalsTotal.Insistence += dweller.Goals.Current.Value.Total.Insistence;
				goalsTotal.Discontent += dweller.Goals.Current.Value.Total.Discontent;
				goalsTotal.DiscontentRangeMinimum += dweller.Goals.Current.Value.Total.DiscontentRange.Minimum;
				goalsTotal.DiscontentRangeMaximum += dweller.Goals.Current.Value.Total.DiscontentRange.Maximum;

				foreach (var value in dweller.Goals.Current.Value.Values)
				{
					var goal = goals[value.Motive];
					
					goal.Insistence += value.Value.Insistence;
					goal.Discontent += value.Value.Discontent;
					goal.DiscontentRangeMinimum += value.Value.DiscontentRange.Minimum;
					goal.DiscontentRangeMaximum += value.Value.DiscontentRange.Maximum;

					goals[value.Motive] = goal;
				}
			}
			
			goalsTotal.Insistence /= result.Population;
			goalsTotal.Discontent /= result.Population;
			goalsTotal.DiscontentRangeMinimum /= result.Population;
			goalsTotal.DiscontentRangeMaximum /= result.Population;

			foreach (var motive in goals.Keys.ToArray())
			{
				var goal = goals[motive];
				
				goal.Insistence /= result.Population;
				goal.Discontent /= result.Population;
				goal.DiscontentRangeMinimum /= result.Population;
				goal.DiscontentRangeMaximum /= result.Population;

				goals[motive] = goal;
			}
			
			result.GoalsAverage = new GoalSnapshot(
				getGoalResult(goalsTotal),
				goals
					.Select(g => (g.Key, getGoalResult(g.Value)))
					.ToArray()
			);

			float getWeightedGoalDiscontentDelta(float lastDiscontent, float nextDiscontent)
			{
				const float PredictedGoalDiscontentWeight = 0.95f;
				return (lastDiscontent * PredictedGoalDiscontentWeight) + (nextDiscontent * (1f - PredictedGoalDiscontentWeight));
			}

			result.GoalsDiscontentDelta = getWeightedGoalDiscontentDelta(GoalsDiscontentDelta, result.GoalsAverage.Total.Discontent);

			var currGoalsByMotive = GoalsDiscontentDeltaByMotive;

			result.GoalsDiscontentDeltaByMotive = result.GoalsAverage.Values
				.Select(
					v => (v.Motive, getWeightedGoalDiscontentDelta(currGoalsByMotive.First(l => l.Motive == v.Motive).DiscontentDelta, v.Value.Discontent))
				)
				.ToArray();

			if (game.Cache.Value.Population != result.Population)
			{
				if (game.Cache.Value.Population < result.Population)
				{
					LastPopulationIncrease = game.SimulationTime.Value;
				}
				else if (result.Population < game.Cache.Value.Population)
				{
					LastPopulationDecrease = game.SimulationTime.Value;
				}

				LastPopulationChange = game.SimulationTime.Value;
			}

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