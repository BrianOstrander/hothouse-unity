using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public enum PooledStates
	{
		Unknown = 0,
		InActive = 10,
		Active = 20
	}

	public interface IPooledModel : IModel, ITransformModel
	{
		#region Serialized
		ListenerProperty<PooledStates> PooledState { get; }
		#endregion
		
		#region Non Serialized
		ListenerProperty<bool> HasPresenter { get; }
		#endregion
	}
	
	public class PooledModel : Model, IPooledModel
	{
		#region Serialized
		[JsonProperty] PooledStates pooledState = PooledStates.InActive;
		[JsonIgnore] public ListenerProperty<PooledStates> PooledState { get; }

		public TransformComponent Transform { get; } = new TransformComponent();
		#endregion
		
		#region Non Serialized
		bool hasPresenter;
		[JsonIgnore] public ListenerProperty<bool> HasPresenter { get; }
		#endregion

		public PooledModel()
		{
			PooledState = new ListenerProperty<PooledStates>(value => pooledState = value, () => pooledState);
			
			HasPresenter = new ListenerProperty<bool>(value => hasPresenter = value, () => hasPresenter);
		}
	}
}