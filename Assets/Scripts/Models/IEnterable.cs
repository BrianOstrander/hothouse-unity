using System.Collections.Generic;
using System.Linq;
using Lunra.Hothouse.Views;
using Lunra.StyxMvp.Models;
using UnityEngine;
using UnityEngine.AI;

namespace Lunra.Hothouse.Models
{
	public interface IEnterable : ILightSensitiveModel
	{
		ListenerProperty<Entrance[]> Entrances { get; }
	}

	public static class IEnterableExtensions
	{
		public static void Recalculate(this IEnterable model)
		{
			model.Recalculate(model.Entrances.Value.Select(e => e.Position));
		}
		
		public static void Recalculate(this IEnterable model, IEnterableView view)
		{
			if (view.Visible) model.Recalculate(view.Entrances);
		}

		static void Recalculate(this IEnterable model, IEnumerable<Vector3> entrances)
		{
			model.Entrances.Value = entrances
				.Select(
					e =>
					{
						var isNavigable = NavMesh.SamplePosition(
							e,
							out _,
							Entrance.RangeMaximum,
							Entrance.DefaultMask
						);
						
						return new Entrance(
							e,
							isNavigable,
							isNavigable && model.IsLit() ? Entrance.States.Available : Entrance.States.NotAvailable
						);
					}
				)
				.ToArray();
		}
	}
}