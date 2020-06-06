using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class GameInteractionModel : InteractionModel
	{
		Interaction.GenericVector3 floorSelection = Interaction.GenericVector3.Default();
		[JsonIgnore] public readonly ListenerProperty<Interaction.GenericVector3> FloorSelection;

		public GameInteractionModel()
		{
			FloorSelection = new ListenerProperty<Interaction.GenericVector3>(value => floorSelection = value, () => floorSelection);
		}
	}
}