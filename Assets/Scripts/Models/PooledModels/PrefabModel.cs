using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public interface IPrefabModel : IPooledModel, ITagModel
	{
		#region Serialized
		ListenerProperty<string> PrefabId { get; }
		ListenerProperty<string> Tag { get; }
		#endregion
	}
	
	public abstract class PrefabModel : PooledModel, IPrefabModel
	{
		[JsonProperty] string prefabId;
		[JsonIgnore] public ListenerProperty<string> PrefabId { get; }
		[JsonProperty] string tag;
		[JsonIgnore] public ListenerProperty<string> Tag { get; }
		
		[JsonProperty] public RoomTransformComponent RoomTransform { get; private set; } = new RoomTransformComponent();
		[JsonProperty] public TagComponent Tags { get; private set; } = new TagComponent();

		public PrefabModel()
		{
			PrefabId = new ListenerProperty<string>(value => prefabId = value, () => prefabId);
			Tag = new ListenerProperty<string>(value => tag = value, () => tag);
			
			AppendComponents(
				RoomTransform,
				Tags
			);
		}
	}
}