using Lunra.StyxMvp.Models;
using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
	public class GameInteractionModel : InteractionModel
	{
		Interaction.Generic floor = Interaction.Generic.Default();
		[JsonIgnore] public readonly ListenerProperty<Interaction.Generic> Floor;

		public GameInteractionModel()
		{
			Floor = new ListenerProperty<Interaction.Generic>(value => floor = value, () => floor);
		}
	}
}