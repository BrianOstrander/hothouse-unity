using System;
using System.Collections.Generic;
using Lunra.Core;
using Lunra.Hothouse.Presenters;
using Lunra.NumberDemon;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class DwellerPoolModel : BasePrefabPoolModel<DwellerModel>
	{
		
		struct GoalCalculationCache
		{
			public Motives Motive { get; }
			public GoalComponent.CalculateGoal CalculateGoal { get; }
			public GoalComponent.CalculateGoalOverflowEffects CalculateGoalOverflowEffects { get; }
			
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
		
		static readonly string[] ValidPrefabIds = new[]
		{
			"default"
		};

		Dictionary<Motives, GoalCalculationCache> goalCalculationCache = new Dictionary<Motives, GoalCalculationCache>();
		GameModel game;
		
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
						(Motives.Heal, calculation?.Invoke(simulationTimeAtMaximum) ?? simulationTimeAtMaximum)
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
						calculateGoalOverflowEffects = getHurtOverflowEffects();
						break;
					case Motives.Eat:
						calculateGoal = insistence => Mathf.Pow(insistence, 5f) - 0.01f;
						calculateGoalOverflowEffects = getHurtOverflowEffects(
							simulationTimeAtMaximum => Mathf.Pow(simulationTimeAtMaximum, 2f)
						);
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
			Vector3 position
		)
		{
			var result = Activate(
				ValidPrefabIds.Random(),
				roomId,
				position,
				RandomRotation,
				model => Reset(model)
			);
			return result;
		}
		
		Quaternion RandomRotation => Quaternion.AngleAxis(DemonUtility.GetNextFloat(0f, 360f), Vector3.up);

		void Reset(DwellerModel model)
		{
			// Agent Properties
			// TODO: NavigationPlan and others may need to be reset...
			model.NavigationVelocity.Value = 40f;
			model.IsDebugging = false;
			model.NavigationForceDistanceMaximum.Value = 4f;
			model.Health.ResetToMaximum(100f);
			model.Inventory.Reset(InventoryCapacity.ByTotalWeight(2));
			
			// Dweller Properties
			model.Job.Value = Jobs.None;
			// model.JobShift.Value = new DayTimeFrame(0.25f, 0.75f);
			// model.JobShift.Value = DayTimeFrame.Maximum;
			model.JobShift.Value = DayTimeFrame.Zero;
			
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

			const float GoalInsistenceVelocity = 0.25f;
			
			model.Goals.Reset(
				new []
				{
					(Motives.Eat, GoalInsistenceVelocity),
					(Motives.Sleep, GoalInsistenceVelocity),
					
					(Motives.Heal, 0f)
				},
				OnCalculateGoal,
				OnCalculateGoalOverflowEffects
			);
			
			model.GoalPromises.Reset();
		}

		GoalResult OnCalculateGoal(
			Motives motive,
			float insistence
		)
		{
			if (goalCalculationCache.TryGetValue(motive, out var cache)) return cache.CalculateGoal(motive, insistence);
			Debug.LogError("No cached calculation for Motive: "+motive);
			return default;
		}
		
		(Motives Motive, float InsistenceModifier)[] OnCalculateGoalOverflowEffects(
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