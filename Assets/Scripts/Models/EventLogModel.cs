using System;
using System.Collections.Generic;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
	public class EventLogModel : Model
	{
		public class Entry
		{
			public string Message;
			public DayTime SimulationTime;
			public InstanceId Source;

			public Entry(
				string message,
				DayTime simulationTime,
				InstanceId source
			)
			{
				Message = message;
				SimulationTime = simulationTime;
				Source = source;
			}

			public override string ToString()
			{
				return "[ " + SimulationTime + " ] " + ShortenId(Source.Id) + ": " + Message;
			}
		}
		
		#region Serialized
		[JsonProperty] Queue<Entry> dwellerEntries = new Queue<Entry>();
		[JsonIgnore] public QueueProperty<Entry> DwellerEntries { get; }
		#endregion
		
		#region Non Serialized
		#endregion

		public EventLogModel()
		{
			DwellerEntries = new QueueProperty<Entry>(dwellerEntries);
		}

		public override string ToString()
		{
			var result = "Dweller:";
			foreach (var entry in DwellerEntries.PeekAll())
			{
				result += "\n - " + entry;
			}

			return result;
		}
	}
}