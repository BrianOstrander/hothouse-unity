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

	public interface IPooledModel : IModel, ITransform
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
		[JsonProperty] Vector3 position = Vector3.zero;
		[JsonIgnore] public ListenerProperty<Vector3> Position { get; }
		[JsonProperty] Quaternion rotation = Quaternion.identity;
		[JsonIgnore] public ListenerProperty<Quaternion> Rotation { get; }
		#endregion
		
		#region Non Serialized
		bool hasPresenter;
		[JsonIgnore] public ListenerProperty<bool> HasPresenter { get; }
		#endregion

		public PooledModel()
		{
			PooledState = new ListenerProperty<PooledStates>(value => pooledState = value, () => pooledState);
			Position = new ListenerProperty<Vector3>(value => position = value, () => position);
			Rotation = new ListenerProperty<Quaternion>(value => rotation = value, () => rotation);
			
			HasPresenter = new ListenerProperty<bool>(value => hasPresenter = value, () => hasPresenter);
		}
	}
}