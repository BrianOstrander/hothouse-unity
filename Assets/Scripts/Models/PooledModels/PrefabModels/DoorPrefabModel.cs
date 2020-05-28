using Lunra.StyxMvp.Models;
using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
    public class DoorPrefabModel : PrefabModel, ILightSensitiveModel, IObligationModel
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
        
        #region Serialized
        [JsonProperty] bool isOpen;
        [JsonIgnore] public ListenerProperty<bool> IsOpen { get; }

        [JsonProperty] Connection roomConnection;
        [JsonIgnore] public ListenerProperty<Connection> RoomConnection { get; }

        [JsonProperty] Obligation[] obligations = new Obligation[0];
        [JsonIgnore] public ListenerProperty<Obligation[]> Obligations { get; }
        
        public LightSensitiveComponent LightSensitive { get; } = new LightSensitiveComponent();
        #endregion
        
        #region Non Serialized
        [JsonIgnore] public EnterableComponent Enterable { get; } = new EnterableComponent();
        #endregion
        
        public DoorPrefabModel()
        {
            IsOpen = new ListenerProperty<bool>(value => isOpen = value, () => isOpen);
            RoomConnection = new ListenerProperty<Connection>(value => roomConnection = value, () => roomConnection);
            Obligations = new ListenerProperty<Obligation[]>(value => obligations = value, () => obligations);
        }

        public bool IsOpenTo(string roomId) => IsOpen.Value && (RoomConnection.Value.RoomId0 == roomId || roomConnection.RoomId1 == roomId);

        public bool IsOpenBetween(string roomId0, string roomId1)
        {
            if (!IsOpen.Value) return false;
            return (roomConnection.RoomId0 == roomId0 && roomConnection.RoomId1 == roomId1) || (roomConnection.RoomId0 == roomId1 && roomConnection.RoomId1 == roomId0);
        }

        public override string ToString()
        {
            return (IsOpen.Value ? "Open" : "Closed") + " { " + RoomConnection.Value.RoomId0 + " , " + RoomConnection.Value.RoomId1 + " } ";
        }
    }
}