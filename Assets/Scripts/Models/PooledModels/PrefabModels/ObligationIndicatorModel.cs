using System.Linq;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class ObligationIndicatorModel : PrefabModel
	{
		#region Serialized
		[JsonProperty] string obligationId;
		[JsonIgnore] public ListenerProperty<string> ObligationId { get; }
		#endregion
		
		#region NonSerialized
		[JsonIgnore] IObligationModel targetInstance;
		[JsonIgnore] public ListenerProperty<IObligationModel> TargetInstance { get; }
		[JsonIgnore] public Obligation ObligationInstance
		{
			get
			{
				if (TargetInstance.Value == null)
				{
					Debug.LogError("Attempting to get obligation instance when target is null");
					return default;
				}
				return TargetInstance.Value.Obligations.All.Value.FirstOrDefault(o => o.Id == ObligationId.Value);
			}
		}
		#endregion
		
		public ObligationIndicatorModel()
		{
			ObligationId = new ListenerProperty<string>(value => obligationId = value, () => obligationId);
			TargetInstance = new ListenerProperty<IObligationModel>(value => targetInstance = value, () => targetInstance);
		}
	}
}