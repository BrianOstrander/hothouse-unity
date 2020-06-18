using System;
using System.Collections.Generic;
using Lunra.Core;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class EffectsModel : Model
	{
		public class Request
		{
			public Vector3 Position { get; }
			public string Id { get; }

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

		[JsonProperty] Queue<Request> queued = new Queue<Request>();
		[JsonIgnore] public QueueProperty<Request> Queued { get; }
		#endregion

		public EffectsModel()
		{
			IsEnabled = new ListenerProperty<bool>(value => isEnabled = value, () => isEnabled);
			
			Queued = new QueueProperty<Request>(queued);
		}
	}
}