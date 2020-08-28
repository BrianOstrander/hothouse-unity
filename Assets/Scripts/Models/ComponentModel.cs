using Lunra.StyxMvp;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
	public interface IParentComponentModel : IModel
	{
		[JsonIgnore] IComponentModel[] Components { get; }
	}
	
	public interface IComponentModel : IModel
	{
		void Bind();
		void UnBind();

		// TODO: This should be renamed possible and called upon the activation of a model too
		void Initialize(
			GameModel game,
			IParentComponentModel model
		);
	}
	
	public abstract class ComponentModel<M> : Model, IComponentModel
		where M : class, IParentComponentModel
	{
		#region Serialized
		#endregion

		#region NonSerialized
		[JsonIgnore] protected GameModel Game { get; private set; }
		[JsonIgnore] protected M Model { get; private set; }
		#endregion

		public virtual void Bind() { }
		public virtual void UnBind() { }

		// TODO: This should be renamed possible and called upon the activation of a model too
		public virtual void Initialize(
			GameModel game,
			IParentComponentModel model
		)
		{
			// TODO: POSSIBLE CHECK IF INITIALIZED ALREADY
			Game = game;
			Model = model as M;
		}
		
		protected void ResetId() => Id.Value = App.M.CreateUniqueId();
	}
	
	public static class ParentComponentModelExtensions
	{
		// TODO: This should be renamed possible and called upon the activation of a model too
		public static void InitializeComponents(
			this IParentComponentModel model,
			GameModel game
		)
		{
			foreach (var component in model.Components) component.Initialize(game, model);
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