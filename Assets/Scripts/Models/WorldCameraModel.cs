using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class WorldCameraModel : Model, ITransformModel
	{
		#region Serialized
		[JsonProperty] bool isEnabled;
		[JsonIgnore] public ListenerProperty<bool> IsEnabled { get; }
		[JsonProperty] float panVelocity;
		[JsonIgnore] public ListenerProperty<float> PanVelocity { get; }
		[JsonProperty] float orbitVelocity;
		[JsonIgnore] public ListenerProperty<float> OrbitVelocity { get; }
		
		public TransformComponent Transform { get; } = new TransformComponent();
		#endregion

		#region Non Serialized
		Camera cameraInstance;
		[JsonIgnore] public ListenerProperty<Camera> CameraInstance { get; }
		#endregion
		
		public WorldCameraModel()
		{
			IsEnabled = new ListenerProperty<bool>(value => isEnabled = value, () => isEnabled);
			PanVelocity = new ListenerProperty<float>(value => panVelocity = value, () => panVelocity);
			OrbitVelocity = new ListenerProperty<float>(value => orbitVelocity = value, () => orbitVelocity);
			
			CameraInstance = new ListenerProperty<Camera>(value => cameraInstance = value, () => cameraInstance);
		}

		public void InitializeComponents() => Components = new[] {Transform};

		public IComponentModel[] Components { get; private set; }
	}
}