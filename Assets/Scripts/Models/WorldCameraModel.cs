using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.WildVacuum.Models
{
	public class WorldCameraModel : Model
	{
		#region Serialized
		[JsonProperty] bool isEnabled;
		[JsonIgnore] public readonly ListenerProperty<bool> IsEnabled;
		#endregion

		#region Non Serialized
		Camera cameraInstance;
		[JsonIgnore] public readonly ListenerProperty<Camera> CameraInstance;
		#endregion
		
		public WorldCameraModel()
		{
			IsEnabled = new ListenerProperty<bool>(value => isEnabled = value, () => isEnabled);
			CameraInstance = new ListenerProperty<Camera>(value => cameraInstance = value, () => cameraInstance);
		}
	}
}