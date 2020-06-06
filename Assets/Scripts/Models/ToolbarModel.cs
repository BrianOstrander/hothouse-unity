using Lunra.StyxMvp.Models;
using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
	public class ToolbarModel : Model
	{
		public enum Tasks
		{
			Unknown = 0,
			None = 10,
			Clearance = 20,
			Construction = 30
		}
		
		#region Serialized
		[JsonProperty] bool isEnabled;
		[JsonIgnore] public ListenerProperty<bool> IsEnabled { get; }

		#endregion
		
		#region Non Serialized
		Tasks task = Tasks.None;
		[JsonIgnore] public ListenerProperty<Tasks> Task { get; }

		BuildingModel building;
		[JsonIgnore] public ListenerProperty<BuildingModel> Building { get; }
		
		Interaction.GenericVector3 clearanceTask;
		[JsonIgnore] public ListenerProperty<Interaction.GenericVector3> ClearanceTask { get; }
		
		Interaction.GenericVector3 constructionTask;
		[JsonIgnore] public ListenerProperty<Interaction.GenericVector3> ConstructionTask { get; }
		#endregion

		public ToolbarModel()
		{
			IsEnabled = new ListenerProperty<bool>(value => isEnabled = value, () => isEnabled);
			
			Task = new ListenerProperty<Tasks>(value => task = value, () => task);
			Building = new ListenerProperty<BuildingModel>(value => building = value, () => building);
			ClearanceTask = new ListenerProperty<Interaction.GenericVector3>(value => clearanceTask = value, () => clearanceTask);
			ConstructionTask = new ListenerProperty<Interaction.GenericVector3>(value => constructionTask = value, () => constructionTask);
		}
	}
}