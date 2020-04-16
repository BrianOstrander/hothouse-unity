using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.WildVacuum.Models
{
    public class WorldCameraModel : Model
    {
        [JsonProperty] bool enabled;
        public readonly ListenerProperty<bool> Enabled;

        public WorldCameraModel()
        {
            Enabled = new ListenerProperty<bool>(value => enabled = value, () => enabled);
        }
    }
}