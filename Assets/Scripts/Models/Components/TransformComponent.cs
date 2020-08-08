using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public interface ITransformModel : IParentComponentModel
	{
		TransformComponent Transform { get; }
	}

	public class TransformComponent : ComponentModel<ITransformModel>
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

	public static class ITransformModelExtensions
	{
		public static float DistanceTo(
			this ITransformModel begin,
			ITransformModel end
		)
		{
			return Vector3.Distance(begin.Transform.Position.Value, end.Transform.Position.Value);
		}
	}
}