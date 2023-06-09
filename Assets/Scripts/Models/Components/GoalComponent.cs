using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public interface IGoalModel : IGoalPromiseModel
	{
		GoalComponent Goals { get; }
	}

	public class GoalComponent : ComponentModel<IGoalModel>
	{
		const float PredictedVelocityPastWeight = 0.95f;
		const float PredictedVelocityRecentWeight = 1f - PredictedVelocityPastWeight;

		public class Cache
		{
			[JsonProperty] public Motives Motive { get; private set; }
			public float Velocity;
			public float VelocityPredicted;
			public float SimulatedTimeAtMaximum;

			public Cache(Motives motive)
			{
				Motive = motive;
			}
		}
		
		public delegate GoalResult CalculateGoal(Motives motive, float insistence);
		public delegate (Motives Motive, float InsistenceModifier)[] CalculateGoalOverflowEffects(Motives motive, float simulationTimeAtMaximum);
	
		#region Serialized
		[JsonProperty] Dictionary<Motives, int> motiveIndexMap = new Dictionary<Motives, int>();
		[JsonProperty] public GoalSnapshot Previous { get; private set; }
		
		[JsonProperty] GoalSnapshot current;
		ListenerProperty<GoalSnapshot> currentListener;
		[JsonIgnore] public ReadonlyProperty<GoalSnapshot> Current { get; }
		
		[JsonProperty] public Cache[] Caches { get; private set; }
		#endregion
		
		#region Non Serialized
		[JsonIgnore] public bool AnyAtMaximum => Caches.Any(c => !Mathf.Approximately(c.SimulatedTimeAtMaximum, 0f));

		DwellerPoolModel definition;
		#endregion

		public GoalComponent()
		{
			Current = new ReadonlyProperty<GoalSnapshot>(
				value => current = value,
				() => current,
				out currentListener
			);
		}

		public override void Initialize(GameModel game, IParentComponentModel model)
		{
			base.Initialize(game, model);

			definition = Game.Dwellers;
		}

		public void Reset(
			DwellerPoolModel definition
		)
		{
			this.definition = definition;
			motiveIndexMap.Clear();
			Caches = new Cache[definition.Velocities.Length];
			(Motives Motive, GoalResult Value)[] values = new (Motives Motive, GoalResult Value)[definition.Velocities.Length];
			
			for (var i = 0; i < definition.Velocities.Length; i++)
			{
				var motive = definition.Velocities[i].Motive;
				
				motiveIndexMap.Add(motive, i);
				
				values[i] = (motive, definition.OnCalculateGoal(motive, 0f));
				
				var cache = new Cache(motive);
				cache.Velocity = definition.Velocities[i].InsistenceModifier;

				Caches[i] = cache;
			}

			currentListener.Value = new GoalSnapshot(
				new GoalResult(
					0f,
					values.Sum(v => v.Value.Discontent),
					new FloatRange(
						values.Sum(v => v.Value.DiscontentRange.Minimum),
						values.Sum(v => v.Value.DiscontentRange.Maximum)
					)
				), 
				values
			);
			Previous = Current.Value;
		}
		
		public override void Bind() => Game.SimulationUpdate += OnGameSimulationUpdate;

		public override void UnBind() => Game.SimulationUpdate -= OnGameSimulationUpdate;

		#region GameModel Events
		void OnGameSimulationUpdate() => Update(Game.SimulationTimeDelta);
		#endregion

		[JsonIgnore] public GoalResult this[Motives motive] => Current.Value.Values.FirstOrDefault(v => v.Motive == motive).Value;

		public void Apply(
			params (Motives Motive, float InsistenceModifier)[] modifiers
		)
		{
			Apply(1f, modifiers);
		}

		public void Apply(
			float multiplier,
			params (Motives Motive, float InsistenceModifier)[] modifiers
		)
		{
			for (var i = 0; i < current.Values.Length; i++)
			{
				var modifier = modifiers.FirstOrDefault(m => m.Motive == current.Values[i].Motive);
				if (modifier.Motive != current.Values[i].Motive) continue;

				current.Values[i].Value = current.Values[i].Value.New(
					current.Values[i].Value.Insistence + (modifier.InsistenceModifier * multiplier)	
				);
			}

			Update(0f, false);
		}

		void Update(
			float simulationDeltaTime,
			bool updatePredictions = true
		)
		{
			if (updatePredictions && Mathf.Approximately(simulationDeltaTime, 0f)) return;
			
			var totalDiscontent = 0f;
			var totalInsistence = 0f;
			
			var values = new (Motives Motive, GoalResult Value)[current.Values.Length];

			var overflowModifiers = new float[current.Values.Length];

			if (updatePredictions)
			{
				foreach (var cacheEntryWithOverflow in Caches.Where(c => !Mathf.Approximately(0f, c.SimulatedTimeAtMaximum)))
				{
					foreach (var modifier in definition.OnCalculateGoalOverflowEffects(cacheEntryWithOverflow.Motive, cacheEntryWithOverflow.SimulatedTimeAtMaximum))
					{
						overflowModifiers[motiveIndexMap[modifier.Motive]] += modifier.InsistenceModifier;
					}
				}
			}
			
			for (var i = 0; i < values.Length; i++)
			{
				var motive = current.Values[i].Motive;
				var baseVelocity = (Caches[i].Velocity + overflowModifiers[i]) * simulationDeltaTime;
				
				values[i].Motive = motive;
				values[i].Value = definition.OnCalculateGoal(
					motive,
					Mathf.Clamp01(
						current.Values[i].Value.Insistence + baseVelocity
					)
				);
				
				totalInsistence += values[i].Value.Insistence;
				totalDiscontent += values[i].Value.Discontent;

				if (updatePredictions)
				{
					var velocitySinceLastTime = (current.Values[i].Value.Insistence - Previous.Values[i].Value.Insistence) / simulationDeltaTime;

					Caches[i].VelocityPredicted =
						(PredictedVelocityPastWeight * Caches[i].VelocityPredicted)
						+ (PredictedVelocityRecentWeight * velocitySinceLastTime);

					if (Mathf.Approximately(1f, values[i].Value.Insistence)) Caches[i].SimulatedTimeAtMaximum += simulationDeltaTime;
					else Caches[i].SimulatedTimeAtMaximum = 0f;
				}

			}

			if (updatePredictions) Previous = Current.Value;
			
			currentListener.Value = new GoalSnapshot(
				new GoalResult(
					totalInsistence / values.Length,
					totalDiscontent,
					Current.Value.Total.DiscontentRange
				),
				values
			);
		}

		public bool TryCalculateDiscontent(
			GoalActivity activity,
			DayTime deltaTime,
			float maximumDiscontent,
			out float discontentWithActivity
		)
		{
			var discontentWithoutActivity = 0f;
			discontentWithActivity = 0f;
			
			for (var i = 0; i < Current.Value.Values.Length; i++)
			{
				var value = Current.Value.Values[i];
				
				var sampleVelocity = Caches[i].VelocityPredicted * deltaTime.TotalTime * activity.Duration.TotalTime;
				
				var discontentModifier = activity.Modifiers
					.FirstOrDefault(m => m.Motive == value.Motive)
					.InsistenceModifier;
			
				discontentWithoutActivity += definition.OnCalculateGoal(
						value.Motive,
						value.Value.Insistence + sampleVelocity
					)
					.Discontent; 
				
				discontentWithActivity += definition.OnCalculateGoal(
						value.Motive,
						value.Value.Insistence + discontentModifier + sampleVelocity
					)
					.Discontent;

				if (maximumDiscontent < discontentWithActivity) return false;
			}

			return discontentWithActivity < discontentWithoutActivity && discontentWithActivity < maximumDiscontent;
		}

		public override string ToString()
		{
			var result = "Goals: " + Current.Value.Total.Discontent.ToString("N2");

			for (var i = 0; i < Current.Value.Values.Length; i++)
			{
				var value = Current.Value.Values[i];
				var cache = Caches[i];

				result += "\n - " + value.Motive + "\t[ " + value.Value.Discontent.ToString("N2")+" / "+value.Value.DiscontentRange.Maximum.ToString("N2")+" ]";

				result += "\tdV: " + cache.VelocityPredicted.ToString("N2");

				if (!Mathf.Approximately(0f, cache.SimulatedTimeAtMaximum))
				{
					result += "\tm: " + cache.SimulatedTimeAtMaximum.ToString("N2");
				}
				
				result += "\n\t" + value.Value.Insistence.ToBarString();
				result += "\n\t" + value.Value.DiscontentNormal.ToBarString();
			}
			
			return result;
		}
	}
}