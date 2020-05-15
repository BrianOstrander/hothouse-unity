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
        [JsonIgnore] public ListenerProperty<bool> IsOpen { get; }

        [JsonProperty] Connection roomConnection;
        [JsonIgnore] public ListenerProperty<Connection> RoomConnection { get; } 
        

        public DoorPrefabModel()
        {
            IsOpen = new ListenerProperty<bool>(value => isOpen = value, () => isOpen);
            RoomConnection = new ListenerProperty<Connection>(value => roomConnection = value, () => roomConnection);
        }
    }
}