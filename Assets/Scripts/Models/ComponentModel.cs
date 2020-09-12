using System;
using Lunra.StyxMvp;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public interface IParentComponentModel : IModel
	{
		[JsonIgnore] IComponentModel[] Components { get; }
	}
	
	public interface IComponentModel : IModel
	{
		void Initialize(
			GameModel game,
			IParentComponentModel model
		);
		
		void Cleanup();
	
		void Bind();
		void UnBind();
	}
	
	public abstract class ComponentModel<M> : Model, IComponentModel
		where M : class, IParentComponentModel
	{
		#region Serialized
		#endregion

		#region NonSerialized
		[JsonIgnore] protected bool IsInitialized { get; private set; }
		[JsonIgnore] protected GameModel Game { get; private set; }
		[JsonIgnore] protected M Model { get; private set; }
		#endregion

		public void Initialize(
			GameModel game,
			IParentComponentModel model
		)
		{
			if (IsInitialized)
			{
				OnReset();
				return;
			}
			
			IsInitialized = true;
			Game = game;
			Model = (model as M) ?? throw new NullReferenceException($"Unable to cast {model.GetType().Name} to {typeof(M).Name}");

			OnInitialize();
		}
		
		/// <summary>
		/// Called once when the component is first initialized.
		/// </summary>
		protected virtual void OnInitialize() { }

		/// <summary>
		/// Called when the component is already initialized.
		/// </summary>
		protected virtual void OnReset() { }
		
		public void Cleanup() => OnCleanup();
		
		/// <summary>
		/// Called when the component is pooled.
		/// </summary>
		protected virtual void OnCleanup() { }

		public virtual void Bind() { }
		public virtual void UnBind() { }
		
		protected void ResetId() => Id.Value = App.M.CreateUniqueId();
	}
	
	public static class ParentComponentModelExtensions
	{
		public static void InitializeComponents(
			this IParentComponentModel model,
			GameModel game
		)
		{
			if (game == null) throw new ArgumentNullException(nameof(game));
			foreach (var component in model.Components) component.Initialize(game, model);
		}

		public static void CleanupComponents(this IParentComponentModel model)
		{
			foreach (var component in model.Components) component.Cleanup();
		}
		
		public static void BindComponents(this IParentComponentModel model)
		{
			foreach (var component in model.Components) component.Bind();
		}
		
		public static void UnBindComponents(this IParentComponentModel model)
		{
			foreach (var component in model.Components) component.UnBind();
		}
	}
}