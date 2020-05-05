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
		
		[JsonProperty] int jobPriority;
		[JsonIgnore] public readonly ListenerProperty<int> JobPriority;
		
		[JsonProperty] Inventory inventory = Models.Inventory.Empty;
		[JsonIgnore] public readonly ListenerProperty<Inventory> Inventory;
		
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
			JobPriority = new ListenerProperty<int>(value => jobPriority = value, () => jobPriority);
			Inventory = new ListenerProperty<Inventory>(value => inventory = value, () => inventory);
			MeleeRange = new ListenerProperty<float>(value => meleeRange = value, () => meleeRange);
			MeleeCooldown = new ListenerProperty<float>(value => meleeCooldown = value, () => meleeCooldown);
			MeleeDamage = new ListenerProperty<float>(value => meleeDamage = value, () => meleeDamage);
		}
	}
}