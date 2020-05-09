using Lunra.StyxMvp.Models;
using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
    public class DoorPrefabModel : PrefabModel
    {
        public class Connection
        {
            public readonly string RoomId0;
            public readonly string RoomId1;

            public Connection(
                string roomId0,
                string roomId1
            )
            {
                RoomId0 = roomId0;
                RoomId1 = roomId1;
            }
        }
        
        [JsonProperty] bool isOpen;
        [JsonIgnore] public readonly ListenerProperty<bool> IsOpen;

        [JsonProperty] Connection roomConnection;
        [JsonIgnore] public readonly ListenerProperty<Connection> RoomConnection; 
        

        public DoorPrefabModel()
        {
            IsOpen = new ListenerProperty<bool>(value => isOpen = value, () => isOpen);
            RoomConnection = new ListenerProperty<Connection>(value => roomConnection = value, () => roomConnection);
        }
    }
}