using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Lunra.Core;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
    public class RoomModel : PrefabModel
    {
        #region Serialized
        [JsonProperty] bool isSpawn;
        [JsonIgnore] public ListenerProperty<bool> IsSpawn { get; }
        [JsonProperty] bool isExit;
        [JsonIgnore] public ListenerProperty<bool> IsExit { get; }
        [JsonProperty] int spawnDistance = int.MaxValue;
        [JsonIgnore] public ListenerProperty<int> SpawnDistance { get; }
        
        [JsonProperty] bool isRevealed;
        [JsonIgnore] public ListenerProperty<bool> IsRevealed { get; }
        [JsonProperty] int revealDistance = int.MaxValue;
        [JsonIgnore] public ListenerProperty<int> RevealDistance { get; }
        #endregion
        
        #region Non Serialized
        ReadOnlyDictionary<string, bool> adjacentRoomIds = (new Dictionary<string, bool>()).ToReadonlyDictionary();
        [JsonIgnore] public ListenerProperty<ReadOnlyDictionary<string, bool>> AdjacentRoomIds { get; }

        [JsonIgnore] public Action<string, bool> UpdateConnection;
        #endregion
        

        public RoomModel()
        {
            IsSpawn = new ListenerProperty<bool>(value => isSpawn = value, () => isSpawn);
            IsExit = new ListenerProperty<bool>(value => isExit = value, () => isExit);
            SpawnDistance = new ListenerProperty<int>(value => spawnDistance = value, () => spawnDistance);
            
            IsRevealed = new ListenerProperty<bool>(value => isRevealed = value, () => isRevealed);
            RevealDistance = new ListenerProperty<int>(value => revealDistance = value, () => revealDistance);
            
            AdjacentRoomIds = new ListenerProperty<ReadOnlyDictionary<string, bool>>(value => adjacentRoomIds = value, () => adjacentRoomIds);
        }
    }
}