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
		
		[JsonProperty] int permittedClaimers;
		[JsonIgnore] public ListenerProperty<int> PermittedClaimers { get; }
		
		[JsonProperty] int maximumClaimers;
		[JsonIgnore] public ListenerProperty<int> MaximumClaimers { get; }
		
		[JsonProperty] public Jobs[] JobRequirements { get; private set; } = new Jobs[0];
		#endregion

		#region Non Serialized
		[JsonIgnore] public bool IsFull => PermittedClaimers.Value <= Claimers.Value.Length;
		#endregion

		public bool Contains(IModel model) => Claimers.Value.Any(instance => instance.Id == model.Id.Value);

		public void Add(IModel model)
		{
			if (model == null)
			{
				Debug.LogError("Cannot add a null model as an owner");
				return;
			}
			Add(InstanceId.New(model));
		}

		public void Add(InstanceId model)
		{
			if (model.IsNull)
			{
				Debug.LogError("Cannot add a null model as an owner");
				return;
			}
			if (Claimers.Value.Any(m => m.Id == model.Id)) return;
			Claimers.Value = Claimers.Value.Append(model).ToArray();
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

		public void Remove(InstanceId model)
		{
			if (model.IsNull)
			{
				Debug.LogError("Cannot remove a null model as an owner");
				return;
			}
			if (Claimers.Value.None(m => m.Id == model.Id)) return;

			Claimers.Value = Claimers.Value.Where(m => m.Id != model.Id).ToArray();
		}
		
		public ClaimComponent()
		{
			Claimers = new ListenerProperty<InstanceId[]>(value => claimers = value, () => claimers);
			PermittedClaimers = new ListenerProperty<int>(value => permittedClaimers = value, () => permittedClaimers);
			MaximumClaimers = new ListenerProperty<int>(value => maximumClaimers = value, () => maximumClaimers);
		}
		
		public void Reset(
			int maximumClaimers,
			params Jobs[] jobRequirements
		)
		{
			Claimers.Value = new InstanceId[0];
			MaximumClaimers.Value = maximumClaimers;
			PermittedClaimers.Value = maximumClaimers;
			JobRequirements = jobRequirements;
		}

		public override string ToString()
		{
			var result = $"Owners [ {Claimers.Value.Length} / {PermittedClaimers.Value} / {MaximumClaimers.Value} ]";

			foreach (var claimer in Claimers.Value) result += "\n - " + ShortenId(claimer.Id);

			return result;
		}
	}
}