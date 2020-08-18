using System;
using Lunra.Core;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
	public class MainMenuModel : Model
	{
		#region Serialized
		#endregion
		
		#region Non Serialized
		[JsonIgnore] public Action<Action<Result<GameModel>>> CreateGame;
		[JsonIgnore] public Action<GameModel> StartGame = ActionExtensions.GetEmpty<GameModel>();
		#endregion

		// public MainMenuModel()
		// {
		// 	
		// }
	}
}