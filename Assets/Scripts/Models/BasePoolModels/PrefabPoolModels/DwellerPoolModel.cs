using System;
using System.Collections.Generic;
using Lunra.Core;
using Lunra.Hothouse.Presenters;
using Lunra.NumberDemon;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class DwellerPoolModel : BasePrefabPoolModel<DwellerModel>
	{
		struct GoalCalculationCache
		{
			[JsonProperty] public Motives Motive { get; private set; }
			[JsonProperty] public GoalComponent.CalculateGoal CalculateGoal { get; private set; }
			[JsonProperty] public GoalComponent.CalculateGoalOverflowEffects CalculateGoalOverflowEffects { get; private set; }
			
			public GoalCalculationCache(
				Motives motive,
				Func<float, float> calculateGoal,
				Func<float, (Motives Motive, float InsistenceModifier)[]> calculateGoaloverflowEffects
			)
			{
				Motive = motive;
				var discontentRange = new FloatRange(
					calculateGoal(0f),
					calculateGoal(1f)
				);
				CalculateGoal = (motives, insistence) => new GoalResult(
					insistence,
					calculateGoal(insistence),
					discontentRange
				);
				CalculateGoalOverflowEffects = (_, simulationTimeAtMaximum) => calculateGoaloverflowEffects(simulationTimeAtMaximum);
			}
		}
		
		static readonly string[] ValidPrefabIds =
		{
			"default"
		};

		GameModel game;
		Dictionary<Motives, GoalCalculationCache> goalCalculationCache = new Dictionary<Motives, GoalCalculationCache>();
		Demon generator = new Demon();
		
		public override void Initialize(GameModel game)
		{
			this.game = game;
			Initialize(
				model => new DwellerPresenter(game, model)	
			);

			var emptyModifiers = new (Motives Motive, float InsistenceModifier)[0];
			Func<float, (Motives Motive, float InsistenceModifier)[]> calculateGoalOverflowEffectsDefault = simulationTimeAtMaximum => emptyModifiers;

			Func<float, (Motives Motive, float InsistenceModifier)[]> getHurtOverflowEffects(Func<float, float> calculation = null)
			{
				return simulationTimeAtMaximum =>
				{
					return new[]
					{
						(Motives.Heal, game.DesireDamageMultiplier.Value * (calculation?.Invoke(simulationTimeAtMaximum) ?? simulationTimeAtMaximum))
					};
				};
			}
			
			foreach (var motive in EnumExtensions.GetValues(Motives.Unknown, Motives.None))
			{
				Func<float, float> calculateGoal;
				var calculateGoalOverflowEffects = calculateGoalOverflowEffectsDefault;
					
				switch (motive)
				{
					case Motives.Sleep:
						calculateGoal = insistence => Mathf.Pow(insistence + 0.05f, 10f) - 0.01f;
						calculateGoalOverflowEffects = getHurtOverflowEffects(
							simulationTimeAtMaximum => Mathf.Max(0f, simulationTimeAtMaximum - 10f)
						);
						break;
					case Motives.Eat:
						calculateGoal = insistence => Mathf.Pow(insistence, 5f) - 0.01f;
						calculateGoalOverflowEffects = getHurtOverflowEffects(
							simulationTimeAtMaximum => Mathf.Pow(Mathf.Max(0f, simulationTimeAtMaximum - 16f), 2f)
						);
						break;
					case Motives.Comfort:
						calculateGoal = insistence => (insistence * 0.2f) - 0.05f;
						break;
					case Motives.Heal:
						calculateGoal = insistence => Mathf.Pow(insistence, 2f);
						break;
					default:
						Debug.LogError("Unrecognized Motive: " + motive);
						calculateGoal = insistence => 0f;
						break;
				}
				
				goalCalculationCache.Add(
					motive,
					new GoalCalculationCache(
						motive,
						insistence => Mathf.Max(0f, calculateGoal(insistence)),
						calculateGoalOverflowEffects
					)
				);
			}
		}

		public DwellerModel Activate(
			string roomId,
			Vector3 position,
			Demon generator = null
		)
		{
			var result = Activate(
				ValidPrefabIds.Random(),
				roomId,
				position,
				RandomRotation,
				model => Reset(model, generator ?? this.generator)
			);
			return result;
		}
		
		Quaternion RandomRotation => Quaternion.AngleAxis(DemonUtility.GetNextFloat(0f, 360f), Vector3.up);

		void Reset(
			DwellerModel model,
			Demon generator
		)
		{
			// Agent Properties
			// TODO: NavigationPlan and others may need to be reset...
			model.NavigationVelocity.Value = 400f; // How many meters per day they can walk...
			model.IsDebugging = false;
			model.NavigationForceDistanceMaximum.Value = 4f;
			model.Health.ResetToMaximum(100f);
			model.Inventory.Reset(InventoryCapacity.ByTotalWeight(2));
			
			// Dweller Properties
			model.Job.Value = Jobs.None;
			model.JobShift.Value = new DayTimeFrame(0f, 0.75f);
			// model.JobShift.Value = DayTimeFrame.Maximum;
			// model.JobShift.Value = DayTimeFrame.Zero;
			
			model.MeleeRange.Value = 0.75f;
			model.MeleeCooldown.Value = 0.5f;
			model.MeleeDamage.Value = 60f;

			model.WithdrawalCooldown.Value = 0.5f;
			model.DepositCooldown.Value = model.WithdrawalCooldown.Value;
			model.TransferDistance.Value = 0.75f;
			
			model.LowRationThreshold.Value = 1;
			model.ObligationDistance.Value = 0.75f;
			model.ObligationMinimumConcentrationDuration.Value = 0.5f;
			
			model.InventoryPromises.Reset();

			model.Goals.Reset(this);
			
			model.GoalPromises.Reset();
			
			model.Name.Value = game.DwellerNames.GetName(generator);
		}

		public (Motives Motive, float InsistenceModifier)[] Velocities => new[]
		{
			(Motives.Eat, 0.25f),
			(Motives.Sleep, 0.25f),
			(Motives.Comfort, 0.25f),

			(Motives.Heal, 0f)
		};

		public GoalResult OnCalculateGoal(
			Motives motive,
			float insistence
		)
		{
			if (goalCalculationCache.TryGetValue(motive, out var cache)) return cache.CalculateGoal(motive, insistence);
			Debug.LogError("No cached calculation for Motive: "+motive);
			return default;
		}
		
		public (Motives Motive, float InsistenceModifier)[] OnCalculateGoalOverflowEffects(
			Motives motive,
			float simulationTimeAtMaximum
		)
		{
			if (goalCalculationCache.TryGetValue(motive, out var cache)) return cache.CalculateGoalOverflowEffects(motive, simulationTimeAtMaximum);
			Debug.LogError("No cached calculation overflow for Motive: "+motive);
			return default;
		}
	}
}