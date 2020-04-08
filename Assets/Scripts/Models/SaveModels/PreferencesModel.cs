using Newtonsoft.Json;

namespace LunraGames.SubLight.Models
{
	// TODO: delete this???
	public class PreferencesModel : SaveModel
	{
		public PreferencesModel()
		{
			SaveType = SaveTypes.Preferences;
		}
	}
}