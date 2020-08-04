using Lunra.StyxMvp.Models;
using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
	public class LevelGenerationModel : Model
	{
		#region Serialized
		[JsonProperty] int seed;
		[JsonIgnore] public ListenerProperty<int> Seed { get; }
		
		public TimestampModel Log { get; } = new TimestampModel();
		#endregion

		public LevelGenerationModel()
		{
			Seed = new ListenerProperty<int>(value => seed = value, () => seed);
		}
	}
}