﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Lunra.Core;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
    public class RoomModel : PrefabModel, IBoundaryModel
    {
        #region Serialized
        [JsonProperty] bool isSpawn;
        [JsonIgnore] public ListenerProperty<bool> IsSpawn { get; }
        [JsonProperty] int spawnDistance;
        [JsonIgnore] public ListenerProperty<int> SpawnDistance { get; }
        
        [JsonProperty] bool isRevealed;
        [JsonIgnore] public ListenerProperty<bool> IsRevealed { get; }
        [JsonProperty] int revealDistance;
        [JsonIgnore] public ListenerProperty<int> RevealDistance { get; }
        [JsonProperty] int[] unpluggedDoors = new int[0];
        [JsonIgnore] public ListenerProperty<int[]> UnPluggedDoors { get; }
        
        public BoundaryComponent Boundary { get; } = new BoundaryComponent();
        #endregion
        
        #region Non Serialized
        ReadOnlyDictionary<string, bool> adjacentRoomIds;
        [JsonIgnore] public ListenerProperty<ReadOnlyDictionary<string, bool>> AdjacentRoomIds { get; }

        [JsonIgnore] public Action<string, bool> UpdateConnection;
        #endregion
        

        public RoomModel()
        {
            IsSpawn = new ListenerProperty<bool>(value => isSpawn = value, () => isSpawn);
            SpawnDistance = new ListenerProperty<int>(value => spawnDistance = value, () => spawnDistance);
            
            IsRevealed = new ListenerProperty<bool>(value => isRevealed = value, () => isRevealed);
            RevealDistance = new ListenerProperty<int>(value => revealDistance = value, () => revealDistance);
            UnPluggedDoors = new ListenerProperty<int[]>(value => unpluggedDoors = value, () => unpluggedDoors);
            
            AdjacentRoomIds = new ListenerProperty<ReadOnlyDictionary<string, bool>>(value => adjacentRoomIds = value, () => adjacentRoomIds);
        }
    }
}