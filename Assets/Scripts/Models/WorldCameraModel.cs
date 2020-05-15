using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class WorldCameraModel : Model
	{
		#region Serialized
		[JsonProperty] bool isEnabled;
		[JsonIgnore] public ListenerProperty<bool> IsEnabled { get; }
		#endregion

		#region Non Serialized
		Camera cameraInstance;
		[JsonIgnore] public ListenerProperty<Camera> CameraInstance { get; }
		#endregion
		
		public WorldCameraModel()
		{
			IsEnabled = new ListenerProperty<bool>(value => isEnabled = value, () => isEnabled);
			CameraInstance = new ListenerProperty<Camera>(value => cameraInstance = value, () => cameraInstance);
		}
	}
}