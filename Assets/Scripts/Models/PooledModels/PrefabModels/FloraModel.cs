﻿using System;
using Lunra.Core;
using Lunra.NumberDemon;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
	public class FloraModel : PrefabModel, IClearableModel
	{
		#region Serialized
		[JsonProperty] string type;
		[JsonIgnore] public ListenerProperty<string> Type { get; }
		
		[JsonProperty] Inventory.Types seed;
		[JsonIgnore] public ListenerProperty<Inventory.Types> Seed { get; }

		[JsonProperty] Interval age;
		[JsonIgnore] public ListenerProperty<Interval> Age { get; }

		[JsonProperty] Interval reproductionElapsed;
		[JsonIgnore] public ListenerProperty<Interval> ReproductionElapsed { get; }
		
		[JsonProperty] FloatRange reproductionRadius;
		[JsonIgnore] public ListenerProperty<FloatRange> ReproductionRadius { get; }
		
		[JsonProperty] int reproductionFailures;
		[JsonIgnore] public ListenerProperty<int> ReproductionFailures { get; }
		
		[JsonProperty] int reproductionFailureLimit;
		[JsonIgnore] public ListenerProperty<int> ReproductionFailureLimit { get; }

		[JsonProperty] float spreadDamage;
		[JsonIgnore] public ListenerProperty<float> SpreadDamage { get; }
		
		[JsonProperty] bool attacksBuildings;
		[JsonIgnore] public ListenerProperty<bool> AttacksBuildings { get; }
		
		public LightSensitiveComponent LightSensitive { get; } = new LightSensitiveComponent();
		public HealthComponent Health { get; } = new HealthComponent();
		public ClearableComponent Clearable { get; } = new ClearableComponent();
		public ObligationComponent Obligations { get; } = new ObligationComponent();
		public EnterableComponent Enterable { get; } = new EnterableComponent(); 
		#endregion
		
		#region Non Serialized
		bool isReproducing;
		[JsonIgnore] public DerivedProperty<bool, int, int> IsReproducing { get; }

		[JsonIgnore] public Func<Demon, FloraModel> TriggerReproduction;
		#endregion
		
		public FloraModel()
		{
			Type = new ListenerProperty<string>(value => type = value, () => type);
			Seed = new ListenerProperty<Inventory.Types>(value => seed = value, () => seed);
			Age = new ListenerProperty<Interval>(value => age = value, () => age);
			ReproductionElapsed = new ListenerProperty<Interval>(value => reproductionElapsed = value, () => reproductionElapsed);
			ReproductionRadius = new ListenerProperty<FloatRange>(value => reproductionRadius = value, () => reproductionRadius);
			ReproductionFailures = new ListenerProperty<int>(value => reproductionFailures = value, () => reproductionFailures);
			ReproductionFailureLimit = new ListenerProperty<int>(value => reproductionFailureLimit = value, () => reproductionFailureLimit);
			SpreadDamage = new ListenerProperty<float>(value => spreadDamage = value, () => spreadDamage);
			AttacksBuildings = new ListenerProperty<bool>(value => attacksBuildings = value, () => attacksBuildings);
			
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