using Lunra.StyxMvp.Models;

namespace Lunra.Hothouse.Models
{
	public interface IRoomTransformModel : ITransformModel
	{
		RoomTransformComponent RoomTransform { get; }
	}

	public class RoomTransformComponent : Model
	{
		public override string ToString() => "RoomId: " + Id.Value;
	}
}