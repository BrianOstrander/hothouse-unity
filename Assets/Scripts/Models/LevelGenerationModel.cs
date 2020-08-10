using Lunra.StyxMvp.Models;
using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
	public class LevelGenerationModel : Model
	{
		#region Serialized
		[JsonProperty] int seed;
		[JsonIgnore] public ListenerProperty<int> Seed { get; }
		
		[JsonProperty] public TimestampModel Log { get; private set; } = new TimestampModel();
		#endregion

		public LevelGenerationModel()
		{
			Seed = new ListenerProperty<int>(value => seed = value, () => seed);
		}
	}
}