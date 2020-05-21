using Lunra.StyxMvp.Models;
using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
	public class ToolbarModel : Model
	{
		public enum States
		{
			Unknown = 0,
			None = 10,
			Gather = 20,
			Construct = 30
		}

		// public struct Mode
		// {
		// 	public readonly States State;
		// 	
		// 	public readonly BuildingStates Construct 
		// }
		
		#region Serialized
		[JsonProperty] bool isEnabled;
		[JsonIgnore] public ListenerProperty<bool> IsEnabled { get; }
		#endregion

		public ToolbarModel()
		{
			IsEnabled = new ListenerProperty<bool>(value => isEnabled = value, () => isEnabled);
		}
	}
}