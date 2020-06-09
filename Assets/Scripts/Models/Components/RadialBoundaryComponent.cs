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
		public Func<Vector3, bool> Contains;
		#endregion
		
		public RadialBoundaryComponent()
		{
			Radius = new ListenerProperty<float>(value => radius = value, () => radius);
		}
	}
	
	public static class IRadialBoundaryExtensions
	{
		public static bool BoundaryContains(this IRadialBoundary model, Vector3 position)
		{
			return Vector3.Distance(model.Transform.Position.Value.NewY(0f), position.NewY(0f)) < model.RadialBoundary.Radius.Value;
		}
	}
}