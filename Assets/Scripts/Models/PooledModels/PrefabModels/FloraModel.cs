using System;
using Lunra.Core;
using Lunra.NumberDemon;
using Lunra.Satchel;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
	public class FloraModel : PrefabModel,
		IClearableModel,
		ILightSensitiveModel
	{
		#region Serialized
		[JsonProperty] string type;
		[JsonIgnore] public ListenerProperty<string> Type { get; }
		
		[JsonProperty] Stack seed;
		[JsonIgnore] public ListenerProperty<Stack> Seed { get; }
		
		[JsonProperty] InstanceId farm;
		[JsonIgnore] public ListenerProperty<InstanceId> Farm { get; }

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
		
		[JsonProperty] public LightSensitiveComponent LightSensitive { get; private set; } = new LightSensitiveComponent();
		[JsonProperty] public HealthComponent Health { get; private set; } = new HealthComponent();
		[JsonProperty] public ClearableComponent Clearable { get; private set; } = new ClearableComponent();
		[JsonProperty] public ObligationComponent Obligations { get; private set; } = new ObligationComponent();
		[JsonProperty] public EnterableComponent Enterable { get; private set; } = new EnterableComponent();
		
		[JsonProperty] public ModifierComponent AgeModifier { get; private set; } = new ModifierComponent();
		[JsonProperty] public ModifierComponent ReproductionModifier { get; private set; } = new ModifierComponent();
		#endregion
		
		#region Non Serialized
		bool isReproducing;
		[JsonIgnore] public DerivedProperty<bool, int, int> IsReproducing { get; }

		[JsonIgnore] public Func<Demon, FloraModel> TriggerReproduction;
		#endregion
		
		public FloraModel()
		{
			Type = new ListenerProperty<string>(value => type = value, () => type);
			Seed = new ListenerProperty<Stack>(value => seed = value, () => seed);
			Farm = new ListenerProperty<InstanceId>(value => farm = value, () => farm);
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
			
			AppendComponents(
				LightSensitive,
				Health,
				Clearable,
				Obligations,
				Enterable,
				AgeModifier,
				ReproductionModifier
			);
		}
	}
}