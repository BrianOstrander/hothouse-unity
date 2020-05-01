using Lunra.StyxMvp.Models;
using Newtonsoft.Json;

namespace Lunra.WildVacuum.Models.AgentModels
{
	public class DwellerModel : AgentModel
	{
		public enum Jobs
		{
			Unknown = 0,
			ClearFlora = 10
		}
		
		#region Serialized
		[JsonProperty] Jobs job;
		[JsonIgnore] public readonly ListenerProperty<Jobs> Job;
		
		[JsonProperty] float meleeRange;
		[JsonIgnore] public readonly ListenerProperty<float> MeleeRange;
		
		[JsonProperty] float meleeCooldown;
		[JsonIgnore] public readonly ListenerProperty<float> MeleeCooldown;
		
		[JsonProperty] float meleeDamage;
		[JsonIgnore] public readonly ListenerProperty<float> MeleeDamage;
		#endregion
		
		#region Non Serialized
		#endregion

		public DwellerModel()
		{
			Job = new ListenerProperty<Jobs>(value => job = value, () => job);
			MeleeRange = new ListenerProperty<float>(value => meleeRange = value, () => meleeRange);
			MeleeCooldown = new ListenerProperty<float>(value => meleeCooldown = value, () => meleeCooldown);
			MeleeDamage = new ListenerProperty<float>(value => meleeDamage = value, () => meleeDamage);
		}
	}
}