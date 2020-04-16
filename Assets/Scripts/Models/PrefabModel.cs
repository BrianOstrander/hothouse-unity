using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.WildVacuum.Models
{
    public class PrefabModel : Model
    {
        [JsonProperty] string prefabId;
        public readonly ListenerProperty<string> PrefabId;

        [JsonProperty] Vector3 position = Vector3.zero;
        public readonly ListenerProperty<Vector3> Position;
        
        [JsonProperty] Quaternion rotation = Quaternion.identity;
        public readonly ListenerProperty<Quaternion> Rotation;

        [JsonProperty] bool enabled;
        public readonly ListenerProperty<bool> Enabled;

        public PrefabModel()
        {
            PrefabId = new ListenerProperty<string>(value => prefabId = value, () => prefabId);
            Position = new ListenerProperty<Vector3>(value => position = value, () => position);
            Rotation = new ListenerProperty<Quaternion>(value => rotation = value, () => rotation);
            Enabled = new ListenerProperty<bool>(value => enabled = value, () => enabled);
        }
    }
}