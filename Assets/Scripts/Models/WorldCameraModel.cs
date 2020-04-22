using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.WildVacuum.Models
{
    public class WorldCameraModel : Model
    {
        [JsonProperty] bool isEnabled;
        public readonly ListenerProperty<bool> IsEnabled;

        public WorldCameraModel()
        {
            IsEnabled = new ListenerProperty<bool>(value => isEnabled = value, () => isEnabled);
        }
    }
}