using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using Lunra.Core;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class BasePoolModel<M> : Model
		where M : class, IPooledModel, new()
	{
		public class Reservoir
		{
			public static Reservoir Default() => new Reservoir(new M[0], new M[0]);
			
			[JsonProperty] public M[] Active { get; private set; }
			[JsonIgnore] public M[] InActive { get; }
			
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
		[JsonProperty] Reservoir all = Reservoir.Default();
		[JsonIgnore] public ListenerProperty<Reservoir> All { get; }
		#endregion
		
		#region Non Serialized
		[JsonIgnore] public M[] AllActive => All.Value.Active;
		[JsonIgnore] public M[] AllInActive => All.Value.InActive;
		[JsonIgnore] public bool IsInitialized { get; private set; }
		public event Action<M> InstantiatePresenter;
		#endregion

		public BasePoolModel()
		{
			All = new ListenerProperty<Reservoir>(value => all = value, () => all);
		}

		protected void Initialize(Action<M> instantiatePresenter)
		{
			if (IsInitialized) throw new Exception("Already initialized");

			InstantiatePresenter = instantiatePresenter;
			
			if (InstantiatePresenter == null) throw new NullReferenceException(nameof(InstantiatePresenter));

			foreach (var model in AllActive)
			{
				if (model.HasPresenter.Value) Debug.LogWarning("Initializing "+nameof(PooledModel)+", but a model already has a presenter, this is an invalid state");
				model.PooledState.ChangedSource += (value, source) => OnPooledState(model, value, source);
				InstantiatePresenter(model);
			}
			
			IsInitialized = true;
		}

		protected M Activate(
			Action<M> initialize = null,
			Func<M, bool> predicate = null
		)
		{
			M result = predicate == null ? All.Value.InActive.FirstOrDefault() : All.Value.InActive.FirstOrDefault(predicate);
			
			if (result == null)
			{
				result = new M();
				result.PooledState.ChangedSource += (value, source) => OnPooledState(result, value, source);
				initialize?.Invoke(result);
				
				All.Value = new Reservoir(
					All.Value.Active.Append(result).ToArray(),
					All.Value.InActive
				);
			}
			else
			{
				result.Id.Value = null;
				initialize?.Invoke(result);
				All.Value = new Reservoir(
					All.Value.Active.Append(result).ToArray(),
					All.Value.InActive.ExceptOne(result).ToArray()
				);
			}
			
			if (string.IsNullOrEmpty(result.Id.Value)) result.Id.Value = Guid.NewGuid().ToString();

			result.PooledState.SetValue(PooledStates.Active, this);
			
			if (IsInitialized && !result.HasPresenter.Value) InstantiatePresenter.Invoke(result);

			return result;
		}
		
		protected void InActivate(params M[] models)
		{
			if (models == null || models.None()) return;

			foreach (var model in models) model.Id.Value = null;
			
			All.Value = new Reservoir(
				All.Value.Active.Except(models).ToArray(),
				All.Value.InActive.Union(models).ToArray()
			);

			foreach (var model in models) model.PooledState.SetValue(PooledStates.InActive, this);
		}

		protected void InActivateAll() => InActivate(AllActive);

		#region Events
		void OnPooledState(
			M model,
			PooledStates pooledState,
			object source
		)
		{
			if (source == this) return;

			switch (pooledState)
			{
				case PooledStates.Active:
					Activate(predicate: m => m == model);
					break;
				case PooledStates.InActive:
					InActivate(model);
					break;
			}
		}
		#endregion
		
		#region Linq Extensions
		public M FirstActive() => AllActive.First();
		public M FirstActive(string id) => FirstActive(m => m.Id.Value == id);
		public M FirstActive(Func<M, bool> predicate) => AllActive.First(predicate);
		public M FirstOrDefaultActive() => AllActive.FirstOrDefault();
		public M FirstOrDefaultActive(string id) => FirstOrDefaultActive(m => m.Id.Value == id);
		public M FirstOrDefaultActive(Func<M, bool> predicate) => AllActive.FirstOrDefault(predicate);
		#endregion
		
		#region Utility
		[JsonIgnore] public (Type ModelType, Func<IEnumerable<IPooledModel>> GetModels) PoolQuery => (typeof(M), () => AllActive);
		#endregion
	}
}