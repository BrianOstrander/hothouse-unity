using Lunra.Core;
using Lunra.StyxMvp.Models;
using Lunra.NumberDemon;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.WildVacuum.Models
{
	public class FloraModel : Model
	{
		public struct Interval
		{
			public static Interval Create(float maximum)
			{
				return new Interval(
					0f,
					maximum
				);
			}

			public static Interval Random(float maximum)
			{
				return new Interval(
					DemonUtility.GetNextFloat(0f, maximum),
					maximum
				);
			}
			
			public readonly float Current;
			public readonly float Maximum;
			public readonly float Normalized;
			public readonly bool IsDone;

			Interval(
				float current,
				float maximum
			)
			{
				Current = current;
				Maximum = maximum;
				Normalized = Mathf.Approximately(maximum, 0f) ? 1f : current / maximum;
				IsDone = Mathf.Approximately(current, maximum);
			}

			public Interval Update(float time)
			{
				return new Interval(
					Mathf.Min(time + Current, Maximum),
					Maximum
				);
			}
		}

		#region Serialized
		// [JsonProperty] string themeId;
		// public readonly ListenerProperty<string> ThemeId;

		[JsonProperty] string roomId;
		public readonly ListenerProperty<string> RoomId;

		[JsonProperty] Vector3 position = Vector3.zero;
		public readonly ListenerProperty<Vector3> Position;
        
		[JsonProperty] Quaternion rotation = Quaternion.identity;
		public readonly ListenerProperty<Quaternion> Rotation;
		
		[JsonProperty] bool isEnabled;
		public readonly ListenerProperty<bool> IsEnabled;
		
		[JsonProperty] Interval age;
		public readonly ListenerProperty<Interval> Age;

		[JsonProperty] Interval reproductionElapsed;
		public readonly ListenerProperty<Interval> ReproductionElapsed;
		
		[JsonProperty] FloatRange reproductionRadius;
		public readonly ListenerProperty<FloatRange> ReproductionRadius;
		
		[JsonProperty] int reproductionFailures;
		public readonly ListenerProperty<int> ReproductionFailures;
		
		[JsonProperty] int reproductionFailureLimit;
		public readonly ListenerProperty<int> ReproductionFailureLimit;

		[JsonProperty] SelectionStates selectionState = SelectionStates.Deselected;
		public readonly ListenerProperty<SelectionStates> SelectionState;

		[JsonProperty] NavigationProximity navigationPoint;
		public readonly ListenerProperty<NavigationProximity> NavigationPoint;
		
		[JsonProperty] float health;
		public readonly ListenerProperty<float> Health;
		
		[JsonProperty] float healthMaximum;
		public readonly ListenerProperty<float> HealthMaximum;
		#endregion
		
		#region Non Serialized
		bool hasPresenter;
		public readonly ListenerProperty<bool> HasPresenter;

		bool isReproducing;
		public readonly DerivedProperty<bool, int, int> IsReproducing;
		#endregion
		
		public FloraModel()
		{
			// ThemeId = new ListenerProperty<string>(value => themeId = value, () => themeId);
			RoomId = new ListenerProperty<string>(value => roomId = value, () => roomId);
			Position = new ListenerProperty<Vector3>(value => position = value, () => position);
			Rotation = new ListenerProperty<Quaternion>(value => rotation = value, () => rotation);
			IsEnabled = new ListenerProperty<bool>(value => isEnabled = value, () => isEnabled);
			Age = new ListenerProperty<Interval>(value => age = value, () => age);
			ReproductionElapsed = new ListenerProperty<Interval>(value => reproductionElapsed = value, () => reproductionElapsed);
			ReproductionRadius = new ListenerProperty<FloatRange>(value => reproductionRadius = value, () => reproductionRadius);
			ReproductionFailures = new ListenerProperty<int>(value => reproductionFailures = value, () => reproductionFailures);
			ReproductionFailureLimit = new ListenerProperty<int>(value => reproductionFailureLimit = value, () => reproductionFailureLimit);
			SelectionState = new ListenerProperty<SelectionStates>(value => selectionState = value, () => selectionState);
			NavigationPoint = new ListenerProperty<NavigationProximity>(value => navigationPoint = value, () => navigationPoint);
			Health = new ListenerProperty<float>(value => health = value, () => health);
			HealthMaximum = new ListenerProperty<float>(value => healthMaximum = value, () => healthMaximum);
			
			HasPresenter = new ListenerProperty<bool>(value => hasPresenter = value, () => hasPresenter);
			IsReproducing = new DerivedProperty<bool, int, int>(
				value => isReproducing = value,
				() => isReproducing,
				(failures, failureLimit) => failures < failureLimit,
				ReproductionFailures,
				ReproductionFailureLimit
			);
		}
	}
}