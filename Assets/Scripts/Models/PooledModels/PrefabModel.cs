using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public interface IPrefabModel : IPooledModel, IRoomTransformModel
	{
		#region Serialized
		ListenerProperty<string> PrefabId { get; }
		#endregion
	}
	
	public abstract class PrefabModel : PooledModel, IPrefabModel
	{
		[JsonProperty] string prefabId;
		[JsonIgnore] public ListenerProperty<string> PrefabId { get; }
		
		public RoomTransformComponent RoomTransform { get; } = new RoomTransformComponent();

		public PrefabModel()
		{
			PrefabId = new ListenerProperty<string>(value => prefabId = value, () => prefabId);
		}
	}
}