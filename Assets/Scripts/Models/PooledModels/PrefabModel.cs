using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.WildVacuum.Models
{
	public abstract class PrefabModel : PooledModel
	{
		[JsonProperty] string prefabId;
		[JsonIgnore] public readonly ListenerProperty<string> PrefabId;

		public PrefabModel()
		{
			PrefabId = new ListenerProperty<string>(value => prefabId = value, () => prefabId);
		}
	}
}