/*
using System;
using System.Linq;
using Lunra.Core;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.WildVacuum.Models
{
	public class BasePoolModel<M> : Model
		where M : PooledModel, new()
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
		public bool IsInitialized { get; private set; }
		public event Action<M> InstantiatePresenter;
		#endregion

		public BasePoolModel()
		{
			All = new ListenerProperty<Reservoir>(value => all = value, () => all);
		}

		public void Initialize(Action<M> instantiatePresenter)
		{
			if (IsInitialized) throw new Exception("Already initialized");

			InstantiatePresenter = instantiatePresenter;
			
			if (InstantiatePresenter == null) throw new NullReferenceException(nameof(InstantiatePresenter));

			foreach (var model in GetActive())
			{
				if (model.HasPresenter.Value) Debug.LogWarning("Initializing "+nameof(PooledModel)+", but a model already has a presenter, this is an invalid state");
				InstantiatePresenter(model);
			}
			
			IsInitialized = true;
		}

		public void InActivate(params M[] models)
		{
			if (models == null || models.None()) return;

			foreach (var model in models) model.Id.Value = null;
			
			All.Value = new Reservoir(
				All.Value.Active.Except(models).ToArray(),
				All.Value.InActive.Union(models).ToArray()
			);

			foreach (var model in models) model.PooledState.Value = PooledStates.InActive;
		}

		public M Activate(
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

			result.PooledState.Value = PooledStates.Active;
			
			if (IsInitialized && !result.HasPresenter.Value) InstantiatePresenter.Invoke(result);

			return result;
		}
	}
}
*/