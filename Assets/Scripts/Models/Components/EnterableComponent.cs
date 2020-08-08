using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
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
	
	public class EnterableComponent : ComponentModel<IEnterableModel>
	{
		#region Non Serialized
		Entrance[] entrances = new Entrance[0];
		[JsonIgnore] public ListenerProperty<Entrance[]> Entrances { get; }
		#endregion

		public EnterableComponent()
		{
			Entrances = new ListenerProperty<Entrance[]>(value => entrances = value, () => entrances);
		}
		
		public bool AnyAvailable() => Entrances.Value.Any(e => e.State == Entrance.States.Available);
		
		public void Reset() => Entrances.Value = new Entrance[0];
	}

	public static class IEnterableExtensions
	{
		static (Vector3 Position, Vector3 Forward) GetEntrance(IEnterableModel model, Vector3 entrance)
		{
			return (model.Transform.Position.Value, (entrance - model.Transform.Position.Value).normalized);
		}
		
		public static void RecalculateEntrances(this IEnterableModel model)
		{
			model.RecalculateEntrances(model.Enterable.Entrances.Value.Select(e => (e.Position, e.Forward)));
		}

		public static void RecalculateEntrances(this IEnterableModel model, Vector3 entrance)
		{
			model.RecalculateEntrances(GetEntrance(model, entrance).ToEnumerable());
		}
		
		public static void RecalculateEntrances(this IEnterableModel model, Vector3[] entrances)
		{
			model.RecalculateEntrances(entrances.Select(e => GetEntrance(model, e)));
		}
		
		public static void RecalculateEntrances(this IEnterableModel model, IEnterableView view)
		{
			if (view.Visible) model.RecalculateEntrances(view.Entrances.Select(e => (e.position, e.forward)));
		}

		static void RecalculateEntrances(this IEnterableModel model, IEnumerable<(Vector3 Position, Vector3 Forward)> entrances)
		{
			model.Enterable.Entrances.Value = entrances
				.Select(
					e =>
					{
						var isNavigable = NavMesh.SamplePosition(
							e.Position,
							out _,
							Entrance.RangeMaximum,
							Entrance.DefaultMask
						);
						
						return new Entrance(
							e.Position,
							e.Forward,
							isNavigable,
							isNavigable && model.LightSensitive.IsLit ? Entrance.States.Available : Entrance.States.NotAvailable
						);
					}
				)
				.ToArray();
		}
	}

	public static class EnterableGameModelExtensions
	{
		public static IEnumerable<IEnterableModel> GetEnterables(
			this GameModel game
		)
		{
			return game.Buildings.AllActive
				.Concat<IEnterableModel>(game.Debris.AllActive)
				.Concat(game.Doors.AllActive)
				.Concat(game.Flora.AllActive)
				.Concat(game.ItemDrops.AllActive)
				.Concat(game.Generators.AllActive);
		}
	}
}