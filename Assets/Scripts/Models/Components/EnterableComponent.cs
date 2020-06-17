using System.Collections.Generic;
using System.Linq;
using Lunra.Hothouse.Views;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.AI;

namespace Lunra.Hothouse.Models
{
	public interface IEnterableModel : ILightSensitiveModel
	{
		EnterableComponent Enterable { get; }
	}
	
	public class EnterableComponent : Model
	{
		#region Non Serialized
		Entrance[] entrances = new Entrance[0];
		[JsonIgnore] public ListenerProperty<Entrance[]> Entrances { get; }
		#endregion

		public EnterableComponent()
		{
			Entrances = new ListenerProperty<Entrance[]>(value => entrances = value, () => entrances);
		}

		public void Reset() => Entrances.Value = new Entrance[0];
	}

	public static class IEnterableExtensions
	{
		public static void RecalculateEntrances(this IEnterableModel model)
		{
			model.RecalculateEntrances(model.Enterable.Entrances.Value.Select(e => e.Position));
		}
		
		public static void RecalculateEntrances(this IEnterableModel model, IEnterableView view)
		{
			if (view.Visible) model.RecalculateEntrances(view.Entrances);
		}

		static void RecalculateEntrances(this IEnterableModel model, IEnumerable<Vector3> entrances)
		{
			model.Enterable.Entrances.Value = entrances
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
							isNavigable && model.LightSensitive.IsLit ? Entrance.States.Available : Entrance.States.NotAvailable
						);
					}
				)
				.ToArray();
		}
	}
}