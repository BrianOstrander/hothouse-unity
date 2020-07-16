using System.Collections.Generic;
using System.Linq;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
	public interface ILightSensitiveModel : IRoomTransformModel
	{
		LightSensitiveComponent LightSensitive { get; }
	}

	public class LightSensitiveComponent : Model
	{
		#region Serialized
		[JsonProperty] float lightLevel;
		[JsonIgnore] public ListenerProperty<float> LightLevel { get; }
		[JsonProperty] string connectedRoomId;
		[JsonIgnore] public ListenerProperty<string> ConnectedRoomId { get; }
		#endregion
		
		#region Non Serialized
		[JsonIgnore] public bool HasConnectedRoomId => !string.IsNullOrEmpty(ConnectedRoomId.Value);
		#endregion

		public LightSensitiveComponent()
		{
			LightLevel = new ListenerProperty<float>(value => lightLevel = value, () => lightLevel);
			ConnectedRoomId = new ListenerProperty<string>(value => connectedRoomId = value, () => connectedRoomId);
		}
		
		[JsonIgnore] public bool IsLit => 0f < LightLevel.Value;
		[JsonIgnore] public bool IsNotLit => !IsLit;

		public void Reset() => LightLevel.Value = 0f;
		
		public override string ToString()
		{
			return "Light Level: " + LightLevel.Value.ToString("N2");
		}
	}
	
	public static class LightSensitiveGameModelExtensions
	{
		public static IEnumerable<ILightSensitiveModel> GetLightSensitives(
			this GameModel game	
		)
		{
			return game.Buildings.AllActive
				.Concat<ILightSensitiveModel>(game.ItemDrops.AllActive)
				.Concat(game.Doors.AllActive)
				.Concat(game.GetClearables());
		}
	}
}