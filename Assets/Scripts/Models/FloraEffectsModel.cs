using System;
using System.Collections.Generic;
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
		[JsonIgnore] public readonly ListenerProperty<bool> IsEnabled;

		[JsonProperty] Queue<Request> spawnQueue = new Queue<Request>();
		[JsonIgnore] public readonly QueueProperty<Request> SpawnQueue;
		
		[JsonProperty] Queue<Request> chopQueue = new Queue<Request>();
		[JsonIgnore] public readonly QueueProperty<Request> ChopQueue;
		#endregion

		public FloraEffectsModel()
		{
			IsEnabled = new ListenerProperty<bool>(value => isEnabled = value, () => isEnabled);
			
			SpawnQueue = new QueueProperty<Request>(spawnQueue);
			ChopQueue = new QueueProperty<Request>(chopQueue);
		}
	}
}