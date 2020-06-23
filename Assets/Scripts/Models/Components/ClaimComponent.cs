using System.Linq;
using Lunra.Core;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public interface IClaimOwnershipModel : IModel
	{
		ClaimComponent Ownership { get; }
	}

	public class ClaimComponent : Model
	{
		#region Serialized
		[JsonProperty] InstanceId[] claimers = new InstanceId[0];
		[JsonIgnore] public ListenerProperty<InstanceId[]> Claimers { get; }
		[JsonProperty] int maximumClaimers;
		[JsonIgnore] public ListenerProperty<int> MaximumClaimers { get; }
		#endregion

		#region Non Serialized
		[JsonIgnore] public bool IsFull => MaximumClaimers.Value <= Claimers.Value.Length;
		#endregion

		public bool Contains(string id) => Claimers.Value.Any(instance => instance.Id == id);

		public void Add(IModel model)
		{
			if (model == null)
			{
				Debug.LogError("Cannot add a null model as an owner");
				return;
			}
			if (Claimers.Value.Any(m => m.Id == model.Id.Value)) return;
			Claimers.Value = Claimers.Value.Append(InstanceId.New(model)).ToArray();
		}

		public void Remove(IModel model)
		{
			if (model == null)
			{
				Debug.LogError("Cannot remove a null model as an owner");
				return;
			}
			if (Claimers.Value.None(m => m.Id == model.Id.Value)) return;

			Claimers.Value = Claimers.Value.Where(m => m.Id != model.Id.Value).ToArray();
		}
		
		public ClaimComponent()
		{
			Claimers = new ListenerProperty<InstanceId[]>(value => claimers = value, () => claimers);
			MaximumClaimers = new ListenerProperty<int>(value => maximumClaimers = value, () => maximumClaimers);
		}
		
		public void Reset()
		{
			Claimers.Value = new InstanceId[0];
			MaximumClaimers.Value = 0;
		}
	}
}