using System;
using System.Linq;
using Lunra.Core;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class TimestampModel : Model
	{
		public struct Entry
		{
			public static Entry New(string message) => new Entry(DateTime.Now, message);
			
			public readonly DateTime Time;
			public readonly string Message;
			
			Entry(
				DateTime time,
				string message
			)
			{
				Time = time;
				Message = message;
			}
		}

		#region Serialized
		[JsonProperty] Entry[] entries = new Entry[0];
		ListenerProperty<Entry[]> entriesListener;
		[JsonIgnore] public ReadonlyProperty<Entry[]> Entries { get; }
		#endregion

		public TimestampModel()
		{
			Entries = new ReadonlyProperty<Entry[]>(
				value => entries = value,
				() => entries,
				out entriesListener
			);
		}
		
		public void Append(params string[] messages)
		{
			entriesListener.Value = Entries.Value
				.Concat(messages.Select(Entry.New))
				.ToArray();
		}

		[JsonIgnore]
		public TimeSpan TotalTime => entries.Length < 2 ? TimeSpan.Zero : (entries.Last().Time - entries.First().Time);

		public TimeSpan GetTimeBetween(string message0, string message1)
		{
			var entry0 = entries.FirstOrDefault(e => e.Message == message0);
			var entry1 = entries.LastOrDefault(e => e.Message == message1);
			
			if (string.IsNullOrEmpty(entry0.Message) || string.IsNullOrEmpty(entry1.Message)) return TimeSpan.Zero;

			return entry1.Time - entry0.Time;
		}
	}
}