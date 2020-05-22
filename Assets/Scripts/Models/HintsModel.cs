using Lunra.StyxMvp.Models;
using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
	public class HintsModel : Model
	{
		#region Serialized
		[JsonProperty] HintCollection[] hintCollections;
		[JsonIgnore] public ListenerProperty<HintCollection[]> HintCollections { get; }

		[JsonProperty] string[] confirmedHintIds = new string[0];
		[JsonIgnore] public ListenerProperty<string[]> ConfirmedHintIds { get; }
		#endregion

		public HintsModel()
		{
			HintCollections = new ListenerProperty<HintCollection[]>(value => hintCollections = value, () => hintCollections);
			ConfirmedHintIds = new ListenerProperty<string[]>(value => confirmedHintIds = value, () => confirmedHintIds);
		}
	}
}