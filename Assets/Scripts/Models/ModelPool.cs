using System;
using System.Linq;
using Lunra.Core;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;

namespace Lunra.WildVacuum.Models
{
	public class ModelPool<M> : Model
		where M : Model, new()
	{
		public class Reservoir
		{
			// ReSharper disable once MemberInitializerValueIgnored
			public readonly M[] Active = new M[0];
			// ReSharper disable once MemberInitializerValueIgnored
			[JsonIgnore] public readonly M[] InActive = new M[0];

			public Reservoir()
			{
				Active = new M[0];
				InActive = new M[0];
			}
			
			public Reservoir(
				M[] active,
				M[] inActive
			)
			{
				Active = active;
				InActive = inActive;
			}
		}
		
		#region Serialized
		[JsonProperty] Reservoir all = new Reservoir();
		[JsonIgnore] public readonly ListenerProperty<Reservoir> All;
		#endregion
		
		#region Non Serialized
		public M[] GetActive() => All.Value.Active;
		public M[] GetInActive() => All.Value.InActive;
		#endregion

		public ModelPool()
		{
			All = new ListenerProperty<Reservoir>(value => all = value, () => all);
		}

		public void InActivate(params M[] models)
		{
			if (models == null || models.None()) return;

			foreach (var model in models) model.Id.Value = null;
			
			All.Value = new Reservoir(
				All.Value.Active.Except(models).ToArray(),
				All.Value.InActive.Union(models).ToArray()
			);
		}

		public void Activate(
			Action<M> initialize = null,
			Func<M, bool> predicate = null
		)
		{
			M result = predicate == null ? All.Value.InActive.FirstOrDefault() : All.Value.InActive.FirstOrDefault(predicate);
			
			if (result == null)
			{
				result = new M();
				initialize?.Invoke(result);
				
				All.Value = new Reservoir(
					All.Value.Active.Append(result).ToArray(),
					All.Value.InActive
				);
			}
			else
			{
				initialize?.Invoke(result);
				All.Value = new Reservoir(
					All.Value.Active.Append(result).ToArray(),
					All.Value.InActive.ExceptOne(result).ToArray()
				);
			}
			
			if (string.IsNullOrEmpty(result.Id.Value)) result.Id.Value = Guid.NewGuid().ToString();
		}
	}
}