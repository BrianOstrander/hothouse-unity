using Lunra.StyxMvp.Models;
using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
	public class BuildingManageModel : Model
	{
		#region Serialized
		#endregion

		#region Non Serialized
		BuildingModel selection;
		[JsonIgnore] public ListenerProperty<BuildingModel> Selection { get; } 
		#endregion

		public BuildingManageModel()
		{
			Selection = new ListenerProperty<BuildingModel>(
				value => selection = value,
				() => selection
			);
		}
	}
}