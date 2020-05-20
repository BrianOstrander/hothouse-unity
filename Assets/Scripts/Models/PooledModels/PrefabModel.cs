using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public interface IPrefabModel : IPooledModel, IRoomTransform
	{
		#region Serialized
		ListenerProperty<string> PrefabId { get; }
		#endregion
	}
	
	public abstract class PrefabModel : PooledModel, IPrefabModel
	{
		[JsonProperty] string prefabId;
		[JsonIgnore] public ListenerProperty<string> PrefabId { get; }
		
		[JsonProperty] string roomId;
		[JsonIgnore] public ListenerProperty<string> RoomId { get; }

		public PrefabModel()
		{
			PrefabId = new ListenerProperty<string>(value => prefabId = value, () => prefabId);
			RoomId = new ListenerProperty<string>(value => roomId = value, () => roomId);
		}
	}
}