using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Lunra.Core;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
    public class RoomPrefabModel : PrefabModel
    {
        #region Serialized
        [JsonProperty] bool isExplored;
        [JsonIgnore] public ListenerProperty<bool> IsExplored { get; }
        #endregion
        
        #region Non Serialized
        ReadOnlyDictionary<string, bool> adjacentRoomIds = (new Dictionary<string, bool>()).ToReadonlyDictionary();
        [JsonIgnore] public ListenerProperty<ReadOnlyDictionary<string, bool>> AdjacentRoomIds { get; }

        [JsonIgnore] public Action<string, bool> UpdateConnection;
        #endregion
        

        public RoomPrefabModel()
        {
            IsExplored = new ListenerProperty<bool>(value => isExplored = value, () => isExplored);
            
            AdjacentRoomIds = new ListenerProperty<ReadOnlyDictionary<string, bool>>(value => adjacentRoomIds = value, () => adjacentRoomIds);
        }
    }
}