using Lunra.StyxMvp.Models;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public interface ILightSensitiveModel : IModel, IRoomPositionModel
	{
		#region Serialized
		ListenerProperty<float> LightLevel { get; }
		#endregion
	}
}