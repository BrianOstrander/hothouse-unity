using Lunra.StyxMvp.Models;
using Newtonsoft.Json;

namespace Lunra.WildVacuum.Models
{
	/// <summary>
	/// All data that is serialized about the game.
	/// </summary>
	public class GameModel : SaveModel
	{
		#region Serialized
		[JsonProperty] public WorldCameraModel Camera = new WorldCameraModel();
		#endregion
		
		#region NonSerialized
		#endregion
	}
}