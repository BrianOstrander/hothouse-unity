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
			public GoalComponent.CalculateGoal Calculation { get; }
			
			public GoalCalculationCache(
				Motives motive,
				Func<float, float> calculation
			)
			{
				Motive = motive;
				var discontentRange = new FloatRange(
					calculation(0f),
					calculation(1f)
				);
				Calculation = (motives, insistence) => new GoalResult(
					insistence,
					calculation(insistence),
					discontentRange
				);
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

			foreach (var motive in EnumExtensions.GetValues(Motives.Unknown, Motives.None))
			{
				Func<float, float> calculation = null;
				switch (motive)
				{
					case Motives.Sleep:
						calculation = insistence => Mathf.Pow(insistence + 0.05f, 10f) - 0.01f;
						break;
					case Motives.Eat:
						calculation = insistence => Mathf.Pow(insistence, 5f) - 0.01f;
						break;
					case Motives.Heal:
						calculation = insistence => Mathf.Pow(insistence, 2f);
						break;
					default:
						Debug.LogError("Unrecognized Motive: " + motive);
						calculation = insistence => 0f;
						break;
				}
				
				goalCalculationCache.Add(
					motive,
					new GoalCalculationCache(
						motive,
						insistence => Mathf.Max(0f, calculation(insistence))
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
			
			model.Desire.Value = Motives.None;
			model.MeleeRange.Value = 0.75f;
			model.MeleeCooldown.Value = 0.5f;
			model.MeleeDamage.Value = 60f;

			model.WithdrawalCooldown.Value = 0.5f;
			model.DepositCooldown.Value = model.WithdrawalCooldown.Value;
			model.TransferDistance.Value = 0.75f;
			
			model.DesireDamage.Value = new Dictionary<Motives, float>
			{
				{ Motives.Eat , 0.3f },
				{ Motives.Sleep , 0.1f }
			};
			model.DesireMissedEmoteTimeout.Value = 2;

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
				OnCalculateGoal
			);
			
			model.GoalPromises.Reset();
		}

		GoalResult OnCalculateGoal(Motives motive, float insistence)
		{
			if (goalCalculationCache.TryGetValue(motive, out var cache)) return cache.Calculation(motive, insistence);
			Debug.LogError("No cached calculation for Motive: "+motive);
			return default;
		}
	}
}