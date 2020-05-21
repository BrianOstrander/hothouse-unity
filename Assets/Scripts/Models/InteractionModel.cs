using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class InteractionModel : Model
	{
		Interaction.Display display = Interaction.Display.Default();
		[JsonIgnore] public readonly ListenerProperty<Interaction.Display> Display;
		Interaction.States displayState = Interaction.Display.Default().State;
		[JsonIgnore] public readonly DerivedProperty<Interaction.States, Interaction.Display> DisplayState;

		Camera camera;
		[JsonIgnore] public readonly ListenerProperty<Camera> Camera;

		public InteractionModel()
		{
			DisplayState = new DerivedProperty<Interaction.States, Interaction.Display>(
				value => displayState = value,
				() => displayState,
				source => source.State,
				Display = new ListenerProperty<Interaction.Display>(value => display = value, () => display)		
			);
			
			Camera = new ListenerProperty<Camera>(value => camera = value, () => camera);
		}
	}
}