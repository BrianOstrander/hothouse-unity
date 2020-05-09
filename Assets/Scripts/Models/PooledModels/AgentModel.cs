using Newtonsoft.Json;
using Lunra.StyxMvp.Models;

namespace Lunra.Hothouse.Models
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

		[JsonProperty] float health;
		[JsonIgnore] public readonly ListenerProperty<float> Health;
		
		[JsonProperty] float healthMaximum;
		[JsonIgnore] public readonly ListenerProperty<float> HealthMaximum;
		#endregion
		
		#region Non Serialized
		[JsonIgnore] public bool IsDebugging { get; set; }
		[JsonIgnore] public AgentContext Context { get; set; } 
		#endregion
		
		public AgentModel()
		{
			NavigationVelocity = new ListenerProperty<float>(value => navigationVelocity = value, () => navigationVelocity);
			NavigationForceDistanceMaximum = new ListenerProperty<float>(value => navigationForceDistanceMaximum = value, () => navigationForceDistanceMaximum);
			NavigationPlan = new ListenerProperty<NavigationPlan>(value => navigationPlan = value, () => navigationPlan);
			Health = new ListenerProperty<float>(value => health = value, () => health);
			HealthMaximum = new ListenerProperty<float>(value => healthMaximum = value, () => healthMaximum);
		}
	}
}