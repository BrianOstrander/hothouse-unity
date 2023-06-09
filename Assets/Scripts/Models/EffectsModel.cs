using System.Collections.Generic;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class EffectsModel : Model
	{
		public class Request
		{
			[JsonProperty] public Vector3 Position { get; private set; }
			[JsonProperty] public string Id { get; private set; }

			public Request(
				Vector3 position,
				string id
			)
			{
				Position = position;
				Id = id;
			}
		}

		#region Serialized
		[JsonProperty] bool isEnabled;
		[JsonIgnore] public ListenerProperty<bool> IsEnabled { get; }

		[JsonProperty] List<Request> queued = new List<Request>();
		[JsonIgnore] public QueueProperty<Request> Queued { get; }
		#endregion

		public EffectsModel()
		{
			IsEnabled = new ListenerProperty<bool>(value => isEnabled = value, () => isEnabled);
			
			Queued = new QueueProperty<Request>(queued);
		}
	}
}