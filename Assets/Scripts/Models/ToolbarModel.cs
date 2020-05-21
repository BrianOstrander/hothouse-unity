using Lunra.StyxMvp.Models;
using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
	public class ToolbarModel : Model
	{
		#region Serialized
		[JsonProperty] bool isEnabled;
		[JsonIgnore] public ListenerProperty<bool> IsEnabled { get; }
		
		// [JsonProperty] Interaction.Generic task;
		// [JsonIgnore] public ListenerProperty<Tasks> Task { get; }
		#endregion

		public ToolbarModel()
		{
			IsEnabled = new ListenerProperty<bool>(value => isEnabled = value, () => isEnabled);
			// Task = new ListenerProperty<Tasks>(value => task = value, () => task);
		}
	}
}