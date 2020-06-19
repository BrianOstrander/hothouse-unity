using System;
using System.Collections.Generic;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
	public class JobManageModel : Model
	{
		#region Serialized
		// [JsonProperty] Queue<Entry> dwellerEntries = new Queue<Entry>();
		// [JsonIgnore] public QueueProperty<Entry> DwellerEntries { get; }
		#endregion
		
		#region Non Serialized
		#endregion

		// public EventLogModel()
		// {
		// 	DwellerEntries = new QueueProperty<Entry>(dwellerEntries);
		// }
	}
}