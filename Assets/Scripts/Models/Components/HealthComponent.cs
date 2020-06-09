using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public interface IHealthModel : IModel, IRoomTransform
	{
		HealthComponent Health { get; }
	}

	public class HealthComponent : Model
	{
		#region Serialized
		[JsonProperty] float current = -1f;
		[JsonIgnore] public ListenerProperty<float> Current { get; }
		[JsonProperty] float maximum;
		[JsonIgnore] public ListenerProperty<float> Maximum { get; }
		#endregion
		
		#region Non Serialized
		[JsonIgnore] public bool IsDamaged => !Mathf.Approximately(Current.Value, Maximum.Value);
		[JsonIgnore] public bool IsDead => Mathf.Approximately(Current.Value, 0f) || Current.Value < 0f;
		[JsonIgnore] public float Normalized => Mathf.Approximately(0f, Maximum.Value) ? 1f : (Current.Value / Maximum.Value);
		#endregion

		public HealthComponent()
		{
			Current = new ListenerProperty<float>(value => current = value, () => current);
			Maximum = new ListenerProperty<float>(value => maximum = value, () => maximum);
		}

		public void Damage(float amount) => Current.Value = Mathf.Clamp(Current.Value - amount, 0f, Maximum.Value);
	}
}