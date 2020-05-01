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
		#endregion
		
		#region Non Serialized
		#endregion

		public DwellerModel()
		{
			Job = new ListenerProperty<Jobs>(value => job = value, () => job);
			MeleeRange = new ListenerProperty<float>(value => meleeRange = value, () => meleeRange);
		}
	}
}