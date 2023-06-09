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
	public interface IEnterableModel : IRoomTransformModel
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

		public override void Bind()
		{
			Game.NavigationMesh.CalculationState.Changed += OnNavigationMeshCalculationState;
		}
		
		public override void UnBind()
		{
			Game.NavigationMesh.CalculationState.Changed -= OnNavigationMeshCalculationState;
		}

		#region Events
		void OnNavigationMeshCalculationState(NavigationMeshModel.CalculationStates calculationState)
		{
			if (calculationState == NavigationMeshModel.CalculationStates.Completed) Model.RecalculateEntrances();
		}
		#endregion
		
		public void Reset() => Entrances.Value = new Entrance[0];
		
		public bool AnyAvailable() => Entrances.Value.Any(e => e.State == Entrance.States.Available);
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
							isNavigable ? Entrance.States.Available : Entrance.States.NotAvailable
						);
					}
				)
				.ToArray();
		}
	}
}