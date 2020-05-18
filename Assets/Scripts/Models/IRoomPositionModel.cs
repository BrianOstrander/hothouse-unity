using Lunra.StyxMvp.Models;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public interface IRoomPositionModel
	{
		#region Serialized
		ListenerProperty<string> RoomId { get; }
		ListenerProperty<Vector3> Position { get; }
		#endregion
	}
}