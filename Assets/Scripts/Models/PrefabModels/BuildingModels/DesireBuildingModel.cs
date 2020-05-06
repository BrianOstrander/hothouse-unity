using System.Collections.Generic;
using Newtonsoft.Json;
using Lunra.StyxMvp.Models;

namespace Lunra.WildVacuum.Models
{
	public class DesireBuildingModel : BuildingModel
	{
		#region Serialized
		[JsonProperty] Dictionary<Desires, float> desireQuality = new Dictionary<Desires, float>();
		[JsonIgnore] public readonly ListenerProperty<Dictionary<Desires, float>> DesireQuality;
		#endregion
		
		#region Non Serialized
		#endregion
		
		public DesireBuildingModel()
		{
			DesireQuality = new ListenerProperty<Dictionary<Desires, float>>(value => desireQuality = value, () => desireQuality);
		}
	}
}