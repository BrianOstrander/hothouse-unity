using System;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
	public class RoomResolverModel : Model
	{
		#region Serialized
		[JsonProperty] int roomCountMinimum;
		[JsonIgnore] public ListenerProperty<int> RoomCountMinimum { get; }
		
		[JsonProperty] int roomCountMaximum;
		[JsonIgnore] public ListenerProperty<int> RoomCountMaximum { get; }

		#endregion
		
		#region Non Serialized
		[JsonIgnore] public Action<Action> Initialize;
		[JsonIgnore] public Action<RoomResolverRequest, Action<RoomResolverResult>> Generate;
		#endregion

		public RoomResolverModel()
		{
			RoomCountMinimum = new ListenerProperty<int>(value => roomCountMinimum = value, () => roomCountMinimum);
			RoomCountMaximum = new ListenerProperty<int>(value => roomCountMaximum = value, () => roomCountMaximum);
		}
	}
}