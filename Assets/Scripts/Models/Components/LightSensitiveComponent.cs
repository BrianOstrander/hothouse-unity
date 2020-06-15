using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public interface ILightSensitiveModel : IModel, IRoomTransform
	{
		LightSensitiveComponent LightSensitive { get; }
	}

	public class LightSensitiveComponent : Model
	{
		#region Serialized
		[JsonProperty] float lightLevel;
		[JsonIgnore] public ListenerProperty<float> LightLevel { get; }
		[JsonProperty] string connectedRoomId;
		[JsonIgnore] public ListenerProperty<string> ConnectedRoomId { get; }
		#endregion
		
		#region Non Serialized
		[JsonIgnore] public bool HasConnectedRoomId => !string.IsNullOrEmpty(ConnectedRoomId.Value);
		#endregion

		public LightSensitiveComponent()
		{
			LightLevel = new ListenerProperty<float>(value => lightLevel = value, () => lightLevel);
			ConnectedRoomId = new ListenerProperty<string>(value => connectedRoomId = value, () => connectedRoomId);
		}
		
		[JsonIgnore] public bool IsLit => 0f < LightLevel.Value;
		[JsonIgnore] public bool IsNotLit => !IsLit;
	}
}