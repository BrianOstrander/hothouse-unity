using Lunra.StyxMvp.Models;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public interface IRoomTransform : ITransform
	{
		#region Serialized
		ListenerProperty<string> RoomId { get; }
		#endregion
	}
}