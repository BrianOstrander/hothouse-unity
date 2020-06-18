using System;
using Lunra.Core;
using Lunra.NumberDemon;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public interface IBoundaryModel : IRoomTransformModel
	{
		BoundaryComponent Boundary { get; }
	}

	public class BoundaryComponent : Model
	{
		#region Serialized
		[JsonProperty] float radius;
		[JsonIgnore] public ListenerProperty<float> Radius { get; }
		#endregion

		#region Non Serialized
		[JsonIgnore] public Func<Vector3, bool> Contains;
		[JsonIgnore] public Func<Demon, Vector3?> RandomPoint;
		#endregion
		
		public BoundaryComponent()
		{
			Radius = new ListenerProperty<float>(value => radius = value, () => radius);
		}
	}
}