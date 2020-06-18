using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class InteractionModel : Model
	{
		Interaction.Display display = Interaction.Display.Default();
		[JsonIgnore] public ListenerProperty<Interaction.Display> Display { get; }
		Interaction.States displayState = Interaction.Display.Default().State;
		[JsonIgnore] public DerivedProperty<Interaction.States, Interaction.Display> DisplayState { get; }
		
		Interaction.GenericVector3 scroll = Interaction.GenericVector3.Default();
		[JsonIgnore] public ListenerProperty<Interaction.GenericVector3> Scroll { get; }

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
			
			Scroll = new ListenerProperty<Interaction.GenericVector3>(value => scroll = value, () => scroll);			
			
			Camera = new ListenerProperty<Camera>(value => camera = value, () => camera);
		}
	}
}