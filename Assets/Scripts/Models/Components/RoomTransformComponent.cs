using Lunra.StyxMvp.Models;

namespace Lunra.Hothouse.Models
{
	public interface IRoomTransform : ITransform
	{
		RoomTransformComponent RoomTransform { get; }
	}
	
	public class RoomTransformComponent : Model { }
}