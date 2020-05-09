﻿using Lunra.Core;
using Lunra.StyxMvp.Models;
using Lunra.NumberDemon;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class FloraModel : PooledModel
	{
		#region Serialized
		// [JsonProperty] string themeId;
		// public readonly ListenerProperty<string> ThemeId;

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

		[JsonProperty] float health;
		[JsonIgnore] public readonly ListenerProperty<float> Health;
		
		[JsonProperty] float healthMaximum;
		[JsonIgnore] public readonly ListenerProperty<float> HealthMaximum;
		
		[JsonProperty] bool markedForClearing;
		[JsonIgnore] public readonly ListenerProperty<bool> MarkedForClearing;
		
		[JsonProperty] Inventory itemDrops = Inventory.Empty;
		[JsonIgnore] public readonly ListenerProperty<Inventory> ItemDrops;
		#endregion
		
		#region Non Serialized
		bool isReproducing;
		[JsonIgnore] public readonly DerivedProperty<bool, int, int> IsReproducing;
		#endregion
		
		public FloraModel()
		{
			// ThemeId = new ListenerProperty<string>(value => themeId = value, () => themeId);
			Age = new ListenerProperty<Interval>(value => age = value, () => age);
			ReproductionElapsed = new ListenerProperty<Interval>(value => reproductionElapsed = value, () => reproductionElapsed);
			ReproductionRadius = new ListenerProperty<FloatRange>(value => reproductionRadius = value, () => reproductionRadius);
			ReproductionFailures = new ListenerProperty<int>(value => reproductionFailures = value, () => reproductionFailures);
			ReproductionFailureLimit = new ListenerProperty<int>(value => reproductionFailureLimit = value, () => reproductionFailureLimit);
			SelectionState = new ListenerProperty<SelectionStates>(value => selectionState = value, () => selectionState);
			Health = new ListenerProperty<float>(value => health = value, () => health);
			HealthMaximum = new ListenerProperty<float>(value => healthMaximum = value, () => healthMaximum);
			MarkedForClearing = new ListenerProperty<bool>(value => markedForClearing = value, () => markedForClearing);
			ItemDrops = new ListenerProperty<Inventory>(value => itemDrops = value, () => itemDrops);
			
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