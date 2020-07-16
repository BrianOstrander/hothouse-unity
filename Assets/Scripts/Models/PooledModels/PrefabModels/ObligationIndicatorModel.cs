using System.Linq;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class ObligationIndicatorModel : PrefabModel
	{
		#region Serialized
		[JsonProperty] Obligation obligation;
		[JsonIgnore] public ListenerProperty<Obligation> Obligation { get; }
		#endregion
		
		#region NonSerialized
		[JsonIgnore] IObligationModel targetInstance;
		[JsonIgnore] public ListenerProperty<IObligationModel> TargetInstance { get; }
		#endregion
		
		public ObligationIndicatorModel()
		{
			Obligation = new ListenerProperty<Obligation>(value => obligation = value, () => obligation);
			TargetInstance = new ListenerProperty<IObligationModel>(value => targetInstance = value, () => targetInstance);
		}
	}
}