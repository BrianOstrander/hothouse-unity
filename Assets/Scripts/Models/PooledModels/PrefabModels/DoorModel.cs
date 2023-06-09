using Lunra.StyxMvp.Models;
using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
    public class DoorModel : PrefabModel, IObligationModel
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

            public override string ToString() => "{ " + RoomId0 + " , " + RoomId1 + " } ";
        }
        
        #region Serialized
        [JsonProperty] bool isOpen;
        [JsonIgnore] public ListenerProperty<bool> IsOpen { get; }

        [JsonProperty] Connection roomConnection;
        [JsonIgnore] public ListenerProperty<Connection> RoomConnection { get; }

        [JsonProperty] public ObligationComponent Obligations { get; private set; } = new ObligationComponent();

        [JsonProperty] public LightSensitiveComponent LightSensitive { get; private set; } = new LightSensitiveComponent();
        #endregion
        
        #region Non Serialized
        [JsonIgnore] public EnterableComponent Enterable { get; } = new EnterableComponent(); // TODO: Why is this not serialized? 
        #endregion
        
        public DoorModel()
        {
            IsOpen = new ListenerProperty<bool>(value => isOpen = value, () => isOpen);
            RoomConnection = new ListenerProperty<Connection>(value => roomConnection = value, () => roomConnection);
            
            AppendComponents(
				Obligations,
                LightSensitive,
                Enterable
            );
        }

        public bool IsConnnecting(string roomId) => RoomConnection.Value.RoomId0 == roomId || roomConnection.RoomId1 == roomId;
        
        public bool IsOpenTo(string roomId) => IsOpen.Value && (RoomConnection.Value.RoomId0 == roomId || roomConnection.RoomId1 == roomId);

        public bool IsOpenBetween(string roomId0, string roomId1)
        {
            if (!IsOpen.Value) return false;
            return (roomConnection.RoomId0 == roomId0 && roomConnection.RoomId1 == roomId1) || (roomConnection.RoomId0 == roomId1 && roomConnection.RoomId1 == roomId0);
        }

        public bool GetConnection(
            string fromRoomId,
            out string toRoomId
        )
        {
            toRoomId = null;
            if (!IsConnnecting(fromRoomId)) return false;
            toRoomId = fromRoomId == RoomConnection.Value.RoomId0 ? RoomConnection.Value.RoomId1 : RoomConnection.Value.RoomId0;
            return true;
        }

        public override string ToString() => (IsOpen.Value ? "Open" : "Closed") + " " + RoomConnection.Value;
    }
}