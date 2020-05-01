using UnityEngine;
using Newtonsoft.Json;
using Lunra.StyxMvp.Models;

namespace Lunra.WildVacuum.Models
{
	public class AgentModel : Model
	{
		public enum States
		{
			Unknown = 0,
			Pooled = 10,
			Visible = 20,
			NotVisible = 30
		}
		
		#region Serialized
		[JsonProperty] States state;
		[JsonIgnore] public readonly ListenerProperty<States> State;
		
		[JsonProperty] Vector3 position = Vector3.zero;
		[JsonIgnore] public readonly ListenerProperty<Vector3> Position;

		[JsonProperty] Quaternion rotation = Quaternion.identity;
		[JsonIgnore] public readonly ListenerProperty<Quaternion> Rotation;

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
			State = new ListenerProperty<States>(value => state = value, () => state);	
			Position = new ListenerProperty<Vector3>(value => position = value, () => position);
			Rotation = new ListenerProperty<Quaternion>(value => rotation = value, () => rotation);
			NavigationVelocity = new ListenerProperty<float>(value => navigationVelocity = value, () => navigationVelocity);
			NavigationForceDistanceMaximum = new ListenerProperty<float>(value => navigationForceDistanceMaximum = value, () => navigationForceDistanceMaximum);
			NavigationPlan = new ListenerProperty<NavigationPlan>(value => navigationPlan = value, () => navigationPlan);
		}
	}
}