using Lunra.StyxMvp.Models;
using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
	public class PopulationModel : Model
	{
		#region Serialized
		[JsonProperty] DayTime nextUpdate = DayTime.Zero;
		[JsonIgnore] public ListenerProperty<DayTime> NextUpdate { get; }
		#endregion
		
		#region Non Serialized
		#endregion

		public PopulationModel()
		{
			NextUpdate = new ListenerProperty<DayTime>(value => nextUpdate = value, () => nextUpdate);
		}
	}
}