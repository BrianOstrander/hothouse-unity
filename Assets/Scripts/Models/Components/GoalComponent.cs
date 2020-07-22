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

	public class GoalComponent : Model
	{
		const float PredictedVelocityPastWeight = 0.95f;
		const float PredictedVelocityRecentWeight = 1f - PredictedVelocityPastWeight;
	
		public delegate GoalResult CalculateGoal(Motives motive, float insistence);
	
		#region Serialized
		[JsonProperty] GoalSnapshot previous;
		
		[JsonProperty] GoalSnapshot current;
		ListenerProperty<GoalSnapshot> currentListener;
		[JsonIgnore] public ReadonlyProperty<GoalSnapshot> Current { get; }
		
		public (Motives Motive, float InsistenceModifier)[] Velocities { get; private set; }
		public (Motives Motive, float InsistenceModifier)[] PredictedVelocities { get; private set; }
		#endregion
		
		#region Non Serialized
		CalculateGoal calculateGoal;
		#endregion

		public GoalComponent()
		{
			Current = new ReadonlyProperty<GoalSnapshot>(
				value => current = value,
				() => current,
				out currentListener
			);
		}

		public void Reset(
			(Motives Motive, float InsistenceModifier)[] velocities,
			CalculateGoal calculateGoal
		)
		{
			Velocities = velocities;
			PredictedVelocities = new (Motives Motive, float InsistenceModifier)[Velocities.Length];
			this.calculateGoal = calculateGoal;

			(Motives Motive, GoalResult Value)[] values = new (Motives Motive, GoalResult Value)[Velocities.Length];
			
			for (var i = 0; i < Velocities.Length; i++)
			{
				var motive = Velocities[i].Motive;
				values[i] = (motive, calculateGoal(motive, 0f));
				PredictedVelocities[i] = (motive, 0f);
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
			previous = Current.Value;
		}

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

		public void Update(
			float simulationDeltaTime,
			bool updatePredictions = true
		)
		{
			var totalDiscontent = 0f;
			var totalInsistence = 0f;
			
			var values = new (Motives Motive, GoalResult Value)[current.Values.Length];

			for (var i = 0; i < values.Length; i++)
			{
				var motive = current.Values[i].Motive;
				var baseVelocity = Velocities[i].InsistenceModifier * simulationDeltaTime;
			
				values[i].Motive = motive;
				values[i].Value = calculateGoal(
					motive,
					Mathf.Clamp01(
						current.Values[i].Value.Insistence + baseVelocity
					)
				);
				
				totalInsistence += values[i].Value.Insistence;
				totalDiscontent += values[i].Value.Discontent;

				if (updatePredictions)
				{
					var velocitySinceLastTime = (current.Values[i].Value.Insistence - previous.Values[i].Value.Insistence) / simulationDeltaTime;

					PredictedVelocities[i].InsistenceModifier =
						(PredictedVelocityPastWeight * PredictedVelocities[i].InsistenceModifier)
						+ (PredictedVelocityRecentWeight * velocitySinceLastTime);
				}
			}

			if (updatePredictions) previous = Current.Value;
			
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
			float deltaTime,
			float maximumDiscontent,
			out float discontentWithActivity
		)
		{
			var discontentWithoutActivity = 0f;
			discontentWithActivity = 0f;
			
			for (var i = 0; i < Current.Value.Values.Length; i++)
			{
				var value = Current.Value.Values[i];
				
				var sampleVelocity = PredictedVelocities[i].InsistenceModifier * deltaTime * activity.Duration.TotalTime;
				
				var discontentModifier = activity.Modifiers
					.FirstOrDefault(m => m.Motive == value.Motive)
					.InsistenceModifier;
			
				discontentWithoutActivity += calculateGoal(
						value.Motive,
						value.Value.Insistence + sampleVelocity
					)
					.Discontent; 
				
				discontentWithActivity += calculateGoal(
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

				result += "\n - " + value.Motive + "\t[ " + value.Value.Discontent.ToString("N2")+" / "+value.Value.DiscontentRange.Maximum.ToString("N2")+" ]";

				result += "\tdV: " + PredictedVelocities[i].InsistenceModifier.ToString("N2");
				
				result += "\n\t" + value.Value.Insistence.ToBarString();
				result += "\n\t" + value.Value.DiscontentNormal.ToBarString();
			}
			
			return result;
		}
	}
}