using Lunra.Core;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
	public class FloraModel : ClearableModel
	{
		#region Serialized
		[JsonProperty] string[] validPrefabIds = new string[0];
		[JsonIgnore] public ListenerProperty<string[]> ValidPrefabIds { get; }
		
		[JsonProperty] FloraSpecies species;
		[JsonIgnore] public ListenerProperty<FloraSpecies> Species { get; }

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
		#endregion
		
		#region Non Serialized
		bool isReproducing;
		[JsonIgnore] public DerivedProperty<bool, int, int> IsReproducing { get; }
		#endregion
		
		public FloraModel()
		{
			ValidPrefabIds = new ListenerProperty<string[]>(value => validPrefabIds = value, () => validPrefabIds);
			Species = new ListenerProperty<FloraSpecies>(value => species = value, () => species);
			Age = new ListenerProperty<Interval>(value => age = value, () => age);
			ReproductionElapsed = new ListenerProperty<Interval>(value => reproductionElapsed = value, () => reproductionElapsed);
			ReproductionRadius = new ListenerProperty<FloatRange>(value => reproductionRadius = value, () => reproductionRadius);
			ReproductionFailures = new ListenerProperty<int>(value => reproductionFailures = value, () => reproductionFailures);
			ReproductionFailureLimit = new ListenerProperty<int>(value => reproductionFailureLimit = value, () => reproductionFailureLimit);
			SpreadDamage = new ListenerProperty<float>(value => spreadDamage = value, () => spreadDamage);
			
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