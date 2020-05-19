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

	public static class LightSensitiveModelExtensions
	{
		public static bool IsLit(this ILightSensitiveModel target) => 0f < target.LightLevel.Value;
		public static bool IsNotLit(this ILightSensitiveModel target) => !target.IsLit();
	}
}