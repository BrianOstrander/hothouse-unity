using Lunra.StyxMvp.Models;
using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
	public class GameInteractionModel : InteractionModel
	{
		Interaction.Generic floorSelection = Interaction.Generic.Default();
		[JsonIgnore] public readonly ListenerProperty<Interaction.Generic> FloorSelection;
		
		Interaction.Generic radialFloorSelection = Interaction.Generic.Default();
		[JsonIgnore] public readonly ListenerProperty<Interaction.Generic> RadialFloorSelection;

		public GameInteractionModel()
		{
			FloorSelection = new ListenerProperty<Interaction.Generic>(value => floorSelection = value, () => floorSelection);
			RadialFloorSelection = new ListenerProperty<Interaction.Generic>(value => radialFloorSelection = value, () => radialFloorSelection);
		}
	}
}