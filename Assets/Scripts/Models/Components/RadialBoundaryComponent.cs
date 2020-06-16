using System;
using Lunra.Core;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public interface IRadialBoundary : IRoomTransform
	{
		RadialBoundaryComponent RadialBoundary { get; }
	}

	public class RadialBoundaryComponent : Model
	{
		#region Serialized
		[JsonProperty] float radius;
		[JsonIgnore] public ListenerProperty<float> Radius { get; }
		#endregion

		#region Non Serialized
		[JsonIgnore] public Func<Vector3, bool> Contains;
		#endregion
		
		public RadialBoundaryComponent()
		{
			Radius = new ListenerProperty<float>(value => radius = value, () => radius);
		}
	}
}