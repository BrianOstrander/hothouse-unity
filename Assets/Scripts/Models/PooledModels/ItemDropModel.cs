using UnityEngine;
using Newtonsoft.Json;
using Lunra.StyxMvp.Models;

namespace Lunra.WildVacuum.Models
{
	public class ItemDropModel : Model
	{
		public enum States
		{
			Unknown = 0,
			Pooled = 10,
			Visible = 20,
			NotVisible = 30
		}
		
		#region Serialized
		[JsonProperty] string roomId;
		[JsonIgnore] public readonly ListenerProperty<string> RoomId;

		[JsonProperty] Vector3 position = Vector3.zero;
		[JsonIgnore] public readonly ListenerProperty<Vector3> Position;
        
		[JsonProperty] Quaternion rotation = Quaternion.identity;
		[JsonIgnore] public readonly ListenerProperty<Quaternion> Rotation;

		[JsonProperty] States state;
		[JsonIgnore] public readonly ListenerProperty<States> State;

		[JsonProperty] Inventory itemDrops = Inventory.Empty;
		[JsonIgnore] public readonly ListenerProperty<Inventory> ItemDrops;
		#endregion
		
		#region Non Serialized
		bool hasPresenter;
		[JsonIgnore] public readonly ListenerProperty<bool> HasPresenter;
		#endregion
		
		public ItemDropModel()
		{
			RoomId = new ListenerProperty<string>(value => roomId = value, () => roomId);
			Position = new ListenerProperty<Vector3>(value => position = value, () => position);
			Rotation = new ListenerProperty<Quaternion>(value => rotation = value, () => rotation);
			State = new ListenerProperty<States>(value => state = value, () => state);
			ItemDrops = new ListenerProperty<Inventory>(value => itemDrops = value, () => itemDrops);
			
			HasPresenter = new ListenerProperty<bool>(value => hasPresenter = value, () => hasPresenter);
		}
	}
}