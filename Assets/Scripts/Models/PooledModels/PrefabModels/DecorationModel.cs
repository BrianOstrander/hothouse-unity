using System;
using Lunra.Core;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class DecorationModel : PrefabModel
	{
		#region Serialized
		[JsonProperty] Vector3 possibleEntrance;
		[JsonIgnore] public ListenerProperty<Vector3> PossibleEntrance { get; }
		
		[JsonProperty] float flow;
		[JsonIgnore] public ListenerProperty<float> Flow { get; }
		#endregion

		public DecorationModel()
		{
			PossibleEntrance = new ListenerProperty<Vector3>(value => possibleEntrance = value, () => possibleEntrance);
			Flow = new ListenerProperty<float>(value => flow = value, () => flow);
		}
	}
}