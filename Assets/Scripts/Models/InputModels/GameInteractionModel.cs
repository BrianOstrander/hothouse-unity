using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class GameInteractionModel : InteractionModel
	{
		Interaction.RoomVector3 floorSelection = Interaction.RoomVector3.Default();
		[JsonIgnore] public ListenerProperty<Interaction.RoomVector3> FloorSelection { get; }
		
		Interaction.GenericVector3 cameraPan = Interaction.GenericVector3.Default();
		[JsonIgnore] public ListenerProperty<Interaction.GenericVector3> CameraPan { get; }
		
		Interaction.GenericFloat cameraOrbit = Interaction.GenericFloat.Default();
		[JsonIgnore] public ListenerProperty<Interaction.GenericFloat> CameraOrbit { get; }

		public GameInteractionModel()
		{
			FloorSelection = new ListenerProperty<Interaction.RoomVector3>(value => floorSelection = value, () => floorSelection);
			CameraPan = new ListenerProperty<Interaction.GenericVector3>(value => cameraPan = value, () => cameraPan);
			CameraOrbit = new ListenerProperty<Interaction.GenericFloat>(value => cameraOrbit = value, () => cameraOrbit);
		}
	}
}