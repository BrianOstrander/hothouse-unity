using System;
using Newtonsoft.Json;
using Lunra.StyxMvp.Models;

namespace Lunra.WildVacuum.Models
{
	/// <summary>
	/// All data that is serialized about the game.
	/// </summary>
	public class GameModel : SaveModel
	{
		#region Serialized
		[JsonProperty] public WorldCameraModel WorldCamera = new WorldCameraModel();

		[JsonProperty] RoomPrefabModel[] rooms = new RoomPrefabModel[0];
		public ListenerProperty<RoomPrefabModel[]> Rooms;

		#endregion

		#region NonSerialized

		#endregion

		public GameModel()
		{
			Rooms = new ListenerProperty<RoomPrefabModel[]>(value => rooms = value, () => rooms);
		}
	}
}