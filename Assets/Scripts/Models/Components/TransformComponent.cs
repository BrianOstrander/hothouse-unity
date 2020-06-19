using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public interface ITransformModel : IModel
	{
		TransformComponent Transform { get; }
	}

	public class TransformComponent : Model
	{
		#region Serialized
		[JsonProperty] Vector3 position;
		[JsonIgnore] public ListenerProperty<Vector3> Position { get; }
		[JsonProperty] Quaternion rotation = Quaternion.identity;
		[JsonIgnore] public ListenerProperty<Quaternion> Rotation { get; }
		#endregion

		public TransformComponent()
		{
			Position = new ListenerProperty<Vector3>(value => position = value, () => position);
			Rotation = new ListenerProperty<Quaternion>(value => rotation = value, () => rotation);
		}
	}
}