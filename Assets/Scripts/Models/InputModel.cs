using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class InputModel : Model
	{
		Input.Display display = Input.Display.Default();
		[JsonIgnore] public readonly ListenerProperty<Input.Display> Display;
		Input.States displayState = Input.Display.Default().State;
		[JsonIgnore] public readonly DerivedProperty<Input.States, Input.Display> DisplayState;

		Camera camera;
		[JsonIgnore] public readonly ListenerProperty<Camera> Camera;

		public InputModel()
		{
			DisplayState = new DerivedProperty<Input.States, Input.Display>(
				value => displayState = value,
				() => displayState,
				source => source.State,
				Display = new ListenerProperty<Input.Display>(value => display = value, () => display)		
			);
			
			Camera = new ListenerProperty<Camera>(value => camera = value, () => camera);
		}
	}
}