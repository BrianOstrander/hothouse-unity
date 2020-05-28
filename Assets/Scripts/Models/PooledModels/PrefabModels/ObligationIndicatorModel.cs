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
		[JsonIgnore] public IObligationModel TargetInstance { get; set; }
		[JsonIgnore] public Obligation ObligationInstance
		{
			get
			{
				if (TargetInstance == null)
				{
					Debug.LogError("Attempting to get obligation instance when target is null");
					return default;
				}
				return TargetInstance.Obligations.All.Value.FirstOrDefault(o => o.Id == ObligationId.Value);
			}
		}
		#endregion
		
		public ObligationIndicatorModel()
		{
			ObligationId = new ListenerProperty<string>(value => obligationId = value, () => obligationId);
		}
	}
}