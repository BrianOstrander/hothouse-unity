using UnityEngine;
using Newtonsoft.Json;
using Lunra.StyxMvp.Models;
using UnityEngine.AI;

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
		#endregion
		
		#region Non Serialized
		NavigationPlan navigationPlan = Models.NavigationPlan.Done();
		[JsonIgnore] public readonly ListenerProperty<NavigationPlan> NavigationPlan;
		#endregion
		
		public AgentModel()
		{
			State = new ListenerProperty<States>(value => state = value, () => state);	
			Position = new ListenerProperty<Vector3>(value => position = value, () => position);
			Rotation = new ListenerProperty<Quaternion>(value => rotation = value, () => rotation);
			NavigationVelocity = new ListenerProperty<float>(value => navigationVelocity = value, () => navigationVelocity);
			
			NavigationPlan = new ListenerProperty<NavigationPlan>(value => navigationPlan = value, () => navigationPlan);
		}
	}
}