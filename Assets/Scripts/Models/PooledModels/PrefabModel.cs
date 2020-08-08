using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public interface IPrefabModel : IPooledModel, IRoomTransformModel
	{
		#region Serialized
		ListenerProperty<string> PrefabId { get; }
		ListenerProperty<string[]> PrefabTags { get; }
		ListenerProperty<string> Tag { get; }
		#endregion
	}
	
	public abstract class PrefabModel : PooledModel, IPrefabModel
	{
		[JsonProperty] string prefabId;
		[JsonIgnore] public ListenerProperty<string> PrefabId { get; }
		[JsonProperty] string[] prefabTags;
		[JsonIgnore] public ListenerProperty<string[]> PrefabTags { get; }
		[JsonProperty] string tag;
		[JsonIgnore] public ListenerProperty<string> Tag { get; }
		
		public RoomTransformComponent RoomTransform { get; } = new RoomTransformComponent();

		public PrefabModel()
		{
			PrefabId = new ListenerProperty<string>(value => prefabId = value, () => prefabId);
			PrefabTags = new ListenerProperty<string[]>(value => prefabTags = value, () => prefabTags);
			Tag = new ListenerProperty<string>(value => tag = value, () => tag);
			
			AppendComponents(RoomTransform);
		}
	}
}