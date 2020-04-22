using Lunra.StyxMvp.Models;
using Newtonsoft.Json;

namespace Lunra.WildVacuum.Models
{
    public class DoorPrefabModel : PrefabModel
    {
        [JsonProperty] bool isOpen;
        public readonly ListenerProperty<bool> IsOpen;

        public DoorPrefabModel()
        {
            IsOpen = new ListenerProperty<bool>(value => isOpen = value, () => isOpen);
        }
    }
}