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
				DayTime simulationTime
			) : this(
				message,
				simulationTime,
				InstanceId.Null()
			) { }
			
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
		[JsonProperty] Stack<Entry> dwellerEntries = new Stack<Entry>();
		[JsonIgnore] public StackProperty<Entry> Dwellers { get; }
		
		[JsonProperty] Stack<Entry> alerts = new Stack<Entry>();
		[JsonIgnore] public StackProperty<Entry> Alerts { get; }
		#endregion
		
		#region Non Serialized
		#endregion

		public EventLogModel()
		{
			Dwellers = new StackProperty<Entry>(dwellerEntries);
			Alerts = new StackProperty<Entry>(alerts);
		}

		public override string ToString()
		{
			var result = "Dweller:";
			foreach (var entry in Dwellers.PeekAll())
			{
				result += "\n - " + entry;
			}
			
			result += "\nAlerts:";
			foreach (var entry in Alerts.PeekAll())
			{
				result += "\n - " + entry;
			}

			return result;
		}
	}
}