using Lunra.StyxMvp.Models;

namespace Lunra.Hothouse.Models
{
	public interface IRoomTransformModel : ITransformModel
	{
		RoomTransformComponent RoomTransform { get; }
	}

	public class RoomTransformComponent : Model
	{
		public override string ToString() => "RoomId: " + ShortId;
	}

	public static class IRoomTransformModelExtensions
	{
		public static bool ShareRoom(
			this IRoomTransformModel model0,
			IRoomTransformModel model1
		)
		{
			return model0.RoomTransform.Id.Value == model1.RoomTransform.Id.Value;
		}
		
		public static bool IsInRoom(
			this IRoomTransformModel model,
			string roomId
		)
		{
			return model.RoomTransform.Id.Value == roomId;
		}
	}
}