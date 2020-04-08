namespace LunraGames.SubLight.Models
{
	/// <summary>
	/// All data that is serialized about the game.
	/// </summary>
	public class GameModel : SaveModel
	{
		public GameModel()
		{
			SaveType = SaveTypes.Game;
		}
	}
}