using UnityEngine;
using Newtonsoft.Json;
using Lunra.StyxMvp.Models;

namespace Lunra.WildVacuum.Models
{
	public abstract class AgentModel : PooledModel
	{
		#region Serialized
		[JsonProperty] float navigationVelocity;
		[JsonIgnore] public readonly ListenerProperty<float> NavigationVelocity;
		
		[JsonProperty] float navigationForceDistanceMaximum;
		[JsonIgnore] public readonly ListenerProperty<float> NavigationForceDistanceMaximum;
		
		[JsonProperty] NavigationPlan navigationPlan = Models.NavigationPlan.Done();
		[JsonIgnore] public readonly ListenerProperty<NavigationPlan> NavigationPlan;
		
		public bool IsDebugging { get; set; }
		#endregion
		
		#region Non Serialized
		#endregion
		
		public AgentModel()
		{
			NavigationVelocity = new ListenerProperty<float>(value => navigationVelocity = value, () => navigationVelocity);
			NavigationForceDistanceMaximum = new ListenerProperty<float>(value => navigationForceDistanceMaximum = value, () => navigationForceDistanceMaximum);
			NavigationPlan = new ListenerProperty<NavigationPlan>(value => navigationPlan = value, () => navigationPlan);
		}
	}
}