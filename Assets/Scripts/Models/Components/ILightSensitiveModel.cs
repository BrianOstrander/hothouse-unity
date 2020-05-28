using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public interface ILightSensitiveModel : IModel, IRoomTransform
	{
		#region Serialized
		LightSensitiveComponent LightSensitive { get; }
		#endregion
	}

	public class LightSensitiveComponent : Model
	{
		#region Serialized
		[JsonProperty] float lightLevel;
		[JsonIgnore] public ListenerProperty<float> LightLevel { get; }
		#endregion

		public LightSensitiveComponent()
		{
			LightLevel = new ListenerProperty<float>(value => lightLevel = value, () => lightLevel);
		}
		
		public bool IsLit() => 0f < LightLevel.Value;
		public bool IsNotLit() => !IsLit();
	}
}