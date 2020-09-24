using Lunra.Hothouse.Models;
using Lunra.Satchel;

namespace Lunra.Hothouse.Services
{
	public abstract class GameProcessor : Processor
	{
		protected GameModel Game { get; private set; }

		public void InitializeGame(GameModel game)
		{
			Game = game;
			OnInitializeGame();
		}
		
		protected void OnInitializeGame() {}
	}
}