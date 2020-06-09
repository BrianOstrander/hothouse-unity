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

		public HealthComponent()
		{
			Current = new ListenerProperty<float>(value => current = value, () => current);
			Maximum = new ListenerProperty<float>(value => maximum = value, () => maximum);
		}
	}
}