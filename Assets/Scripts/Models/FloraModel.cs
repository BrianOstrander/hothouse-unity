using System;
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

		public enum States
		{
			Unknown = 0,
			Pooled = 10,
			Visible = 20,
			NotVisible = 30
		}
		
		#region Serialized
		// [JsonProperty] string themeId;
		// public readonly ListenerProperty<string> ThemeId;

		[JsonProperty] string roomId;
		[JsonIgnore] public readonly ListenerProperty<string> RoomId;

		[JsonProperty] Vector3 position = Vector3.zero;
		[JsonIgnore] public readonly ListenerProperty<Vector3> Position;
        
		[JsonProperty] Quaternion rotation = Quaternion.identity;
		[JsonIgnore] public readonly ListenerProperty<Quaternion> Rotation;

		[JsonProperty] States state;
		[JsonIgnore] public readonly ListenerProperty<States> State;
		
		[JsonProperty] Interval age;
		[JsonIgnore] public readonly ListenerProperty<Interval> Age;

		[JsonProperty] Interval reproductionElapsed;
		[JsonIgnore] public readonly ListenerProperty<Interval> ReproductionElapsed;
		
		[JsonProperty] FloatRange reproductionRadius;
		[JsonIgnore] public readonly ListenerProperty<FloatRange> ReproductionRadius;
		
		[JsonProperty] int reproductionFailures;
		[JsonIgnore] public readonly ListenerProperty<int> ReproductionFailures;
		
		[JsonProperty] int reproductionFailureLimit;
		[JsonIgnore] public readonly ListenerProperty<int> ReproductionFailureLimit;

		[JsonProperty] SelectionStates selectionState = SelectionStates.Deselected;
		[JsonIgnore] public readonly ListenerProperty<SelectionStates> SelectionState;

		[JsonProperty] NavigationProximity navigationPoint;
		[JsonIgnore] public readonly ListenerProperty<NavigationProximity> NavigationPoint;
		
		[JsonProperty] float health;
		[JsonIgnore] public readonly ListenerProperty<float> Health;
		
		[JsonProperty] float healthMaximum;
		[JsonIgnore] public readonly ListenerProperty<float> HealthMaximum;
		
		[JsonProperty] bool markedForClearing;
		[JsonIgnore] public readonly ListenerProperty<bool> MarkedForClearing;
		#endregion
		
		#region Non Serialized
		bool hasPresenter;
		[JsonIgnore] public readonly ListenerProperty<bool> HasPresenter;

		bool isReproducing;
		[JsonIgnore] public readonly DerivedProperty<bool, int, int> IsReproducing;
		#endregion
		
		public FloraModel()
		{
			// ThemeId = new ListenerProperty<string>(value => themeId = value, () => themeId);
			RoomId = new ListenerProperty<string>(value => roomId = value, () => roomId);
			Position = new ListenerProperty<Vector3>(value => position = value, () => position);
			Rotation = new ListenerProperty<Quaternion>(value => rotation = value, () => rotation);
			State = new ListenerProperty<States>(value => state = value, () => state);
			Age = new ListenerProperty<Interval>(value => age = value, () => age);
			ReproductionElapsed = new ListenerProperty<Interval>(value => reproductionElapsed = value, () => reproductionElapsed);
			ReproductionRadius = new ListenerProperty<FloatRange>(value => reproductionRadius = value, () => reproductionRadius);
			ReproductionFailures = new ListenerProperty<int>(value => reproductionFailures = value, () => reproductionFailures);
			ReproductionFailureLimit = new ListenerProperty<int>(value => reproductionFailureLimit = value, () => reproductionFailureLimit);
			SelectionState = new ListenerProperty<SelectionStates>(value => selectionState = value, () => selectionState);
			NavigationPoint = new ListenerProperty<NavigationProximity>(value => navigationPoint = value, () => navigationPoint);
			Health = new ListenerProperty<float>(value => health = value, () => health);
			HealthMaximum = new ListenerProperty<float>(value => healthMaximum = value, () => healthMaximum);
			MarkedForClearing = new ListenerProperty<bool>(value => markedForClearing = value, () => markedForClearing);
			
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