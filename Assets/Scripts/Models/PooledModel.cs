using System.Linq;
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

	public interface IPooledModel : ITransformModel
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

		[JsonProperty] public TransformComponent Transform { get; private set; } = new TransformComponent();
		#endregion
		
		#region Non Serialized
		bool hasPresenter;
		[JsonIgnore] public ListenerProperty<bool> HasPresenter { get; }
		
		[JsonIgnore] public IComponentModel[] Components { get; private set; } = new IComponentModel[0];
		#endregion

		public PooledModel()
		{
			PooledState = new ListenerProperty<PooledStates>(value => pooledState = value, () => pooledState);
			
			HasPresenter = new ListenerProperty<bool>(value => hasPresenter = value, () => hasPresenter);
			
			AppendComponents(Transform);
		}
		
		protected void AppendComponents(params IComponentModel[] components) => Components = Components.Concat(components).ToArray();
	}
}