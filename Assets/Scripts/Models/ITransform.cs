using Lunra.StyxMvp.Models;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public interface ITransform
	{
		#region Serialized
		ListenerProperty<Vector3> Position { get; }
		ListenerProperty<Quaternion> Rotation { get; }
		#endregion
	}
}