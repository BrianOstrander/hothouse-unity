using System;
using Lunra.Core;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.WildVacuum.Models
{
	public class FloraEffectsModel : Model
	{
		public struct Request
		{
			public readonly Vector3 Position;

			public Request(
				Vector3 position
			)
			{
				Position = position;
			}
		}

		#region Serialized
		[JsonProperty] bool isEnabled;
		public readonly ListenerProperty<bool> IsEnabled;
		#endregion
		
		#region Non Serialized
		public Action<Request> Spawn = ActionExtensions.GetEmpty<Request>();
		public Action<Request> Chop = ActionExtensions.GetEmpty<Request>();
		#endregion

		public FloraEffectsModel()
		{
			IsEnabled = new ListenerProperty<bool>(value => isEnabled = value, () => isEnabled);
		}
	}
}