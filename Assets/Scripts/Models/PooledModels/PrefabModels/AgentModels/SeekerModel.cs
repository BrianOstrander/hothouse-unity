using Lunra.StyxMvp.Models;
using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
	public class SeekerModel : AgentModel
	{
		#region Serialized
		[JsonProperty] float damageRange;
		[JsonIgnore] public ListenerProperty<float> DamageRange { get; }

		[JsonProperty] float damageAmount;
		[JsonIgnore] public ListenerProperty<float> DamageAmount { get; }
		
		[JsonProperty] Damage.Types damageType;
		[JsonIgnore] public ListenerProperty<Damage.Types> DamageType { get; }
		
		[JsonProperty] InstanceId.Types[] validTargets = new InstanceId.Types[0];
		[JsonIgnore] public ListenerProperty<InstanceId.Types[]> ValidTargets { get; }
		#endregion
		
		#region Non Serialized
		#endregion

		public SeekerModel()
		{
			DamageRange = new ListenerProperty<float>(value => damageRange = value, () => damageRange);
			DamageAmount = new ListenerProperty<float>(value => damageAmount = value, () => damageAmount);
			DamageType = new ListenerProperty<Damage.Types>(value => damageType = value, () => damageType);
			ValidTargets = new ListenerProperty<InstanceId.Types[]>(value => validTargets = value, () => validTargets);
		}
	}
}