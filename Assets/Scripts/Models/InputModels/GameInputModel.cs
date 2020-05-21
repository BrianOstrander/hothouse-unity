using Lunra.StyxMvp.Models;
using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
	public class GameInputModel : InputModel
	{
		Input.Generic floor = Input.Generic.Default();
		[JsonIgnore] public readonly ListenerProperty<Input.Generic> Floor;

		public GameInputModel()
		{
			Floor = new ListenerProperty<Input.Generic>(value => floor = value, () => floor);
		}
	}
}