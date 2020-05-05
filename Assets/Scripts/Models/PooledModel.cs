using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.WildVacuum.Models
{
	public enum PooledStates
	{
		Unknown = 0,
		Pooled = 10,
		Visible = 20,
		NotVisible = 30
	}
	
	public class PooledModel : Model
	{
		
		#region Serialized
		[JsonProperty] string roomId;
		[JsonIgnore] public readonly ListenerProperty<string> RoomId;
		
		[JsonProperty] Vector3 position = Vector3.zero;
		[JsonIgnore] public readonly ListenerProperty<Vector3> Position;
		
		[JsonProperty] Quaternion rotation = Quaternion.identity;
		[JsonIgnore] public readonly ListenerProperty<Quaternion> Rotation;
		
		[JsonProperty] PooledStates pooledState;
		[JsonIgnore] public readonly ListenerProperty<PooledStates> PooledState;
		#endregion
		
		#region Non Serialized
		bool hasPresenter;
		[JsonIgnore] public readonly ListenerProperty<bool> HasPresenter;
		#endregion

		public PooledModel()
		{
			// ThemeId = new ListenerProperty<string>(value => themeId = value, () => themeId);
			RoomId = new ListenerProperty<string>(value => roomId = value, () => roomId);
			Position = new ListenerProperty<Vector3>(value => position = value, () => position);
			Rotation = new ListenerProperty<Quaternion>(value => rotation = value, () => rotation);
			PooledState = new ListenerProperty<PooledStates>(value => pooledState = value, () => pooledState);

			HasPresenter = new ListenerProperty<bool>(value => hasPresenter = value, () => hasPresenter);
		}
	}
}