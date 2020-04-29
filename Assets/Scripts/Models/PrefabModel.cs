using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.WildVacuum.Models
{
	public class PrefabModel : Model
	{
		[JsonProperty] string prefabId;
		[JsonIgnore] public readonly ListenerProperty<string> PrefabId;

		[JsonProperty] Vector3 position = Vector3.zero;
		[JsonIgnore] public readonly ListenerProperty<Vector3> Position;

		[JsonProperty] Quaternion rotation = Quaternion.identity;
		[JsonIgnore] public readonly ListenerProperty<Quaternion> Rotation;

		[JsonProperty] bool isEnabled;
		[JsonIgnore] public readonly ListenerProperty<bool> IsEnabled;

		public PrefabModel()
		{
			PrefabId = new ListenerProperty<string>(value => prefabId = value, () => prefabId);
			Position = new ListenerProperty<Vector3>(value => position = value, () => position);
			Rotation = new ListenerProperty<Quaternion>(value => rotation = value, () => rotation);
			IsEnabled = new ListenerProperty<bool>(value => isEnabled = value, () => isEnabled);
		}
	}
}