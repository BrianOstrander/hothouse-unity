using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Lunra.Core;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public interface IGoalModel : IRoomTransformModel
	{
		GoalComponent Goals { get; }
	}

	public class GoalComponent : Model
	{
		public delegate GoalResult CalculateGoal(Motives motive, float insistence);
	
		#region Serialized
		[JsonProperty] GoalSnapshot current;
		ListenerProperty<GoalSnapshot> currentListener;
		[JsonIgnore] public ReadonlyProperty<GoalSnapshot> Current { get; }
		
		public float Velocity { get; private set; }
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
			float velocity,
			Motives[] motives,
			CalculateGoal calculateGoal
		)
		{
			Velocity = velocity;
			this.calculateGoal = calculateGoal;

			(Motives Motive, GoalResult Value)[] values = motives
				.Select(m => (m, calculateGoal(m, 0f)))
				.ToArray();
			
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
		}

		public void Update(float deltaTime)
		{
			var totalDiscontent = 0f;
			var totalInsistence = 0f;
			
			var values = new (Motives Motive, GoalResult Value)[current.Values.Length];

			for (var i = 0; i < values.Length; i++)
			{
				values[i].Motive = current.Values[i].Motive;
				values[i].Value = calculateGoal(
					current.Values[i].Motive,
					Mathf.Clamp01(current.Values[i].Value.Insistence + (Velocity * deltaTime))
				);

				totalInsistence += values[i].Value.Insistence;
				totalDiscontent += values[i].Value.Discontent;
			}
			
			currentListener.Value = new GoalSnapshot(
				new GoalResult(
					totalInsistence / values.Length,
					totalDiscontent,
					Current.Value.Total.DiscontentRange
				),
				values
			);
		}
		
		bool TryCalculateDiscontent(
			IDictionary<Motives, GoalResult> beginValues,
			float deltaTime,
			float maximumDiscontent,
			out float discontent
		)
		{
			discontent = 0f;
			var sampleVelocity = Velocity * deltaTime;
			
			foreach (var kv in beginValues)
			{
				discontent += calculateGoal(kv.Key, kv.Value.Insistence + sampleVelocity).Discontent;

				if (maximumDiscontent < discontent) return false;
			}

			return discontent <= maximumDiscontent;
		}

		public override string ToString()
		{
			var result = "Goals: " + Current.Value.Total.DiscontentNormal.ToString("N2");

			foreach (var value in Current.Value.Values)
			{

				result += "\n - " + value.Motive + "\t[ " + value.Value.Discontent.ToString("N2")+" / "+value.Value.DiscontentRange.Maximum.ToString("N2")+" ]";

				result += "\n\t" + value.Value.Insistence.ToBarString();
				result += "\n\t" + value.Value.DiscontentNormal.ToBarString();
			}
			
			return result;
		}
	}
}