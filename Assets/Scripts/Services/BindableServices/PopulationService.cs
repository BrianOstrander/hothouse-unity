using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.StyxMvp.Services;
using UnityEngine;

namespace Lunra.Hothouse.Services
{
	public class PopulationService : BindableService<GameModel>
	{
		public PopulationService(GameModel model) : base(model) { }

		protected override void Bind()
		{
			IncrementNextUpdate();
			
			Model.SimulationUpdate += OnGameSimulationUpdate;
		}

		protected override void UnBind()
		{
			Model.SimulationUpdate -= OnGameSimulationUpdate;
		}
		
		// TODO: Don't hardcode this...
		void IncrementNextUpdate() => Model.Population.NextUpdate.Value = Model.SimulationTime.Value + new DayTime(5); 
		
		#region GameModel Events
		void OnGameSimulationUpdate()
		{
			if (Model.SimulationTime.Value < Model.Population.NextUpdate.Value) return;

			IncrementNextUpdate();

			var motivesNotMet = Model.Cache.Value.GoalsAverage.Values
				.Where(g => 0.5f < g.Value.DiscontentNormal)
				.ToArray();
			
			// TODO: Don't hardcode this...
			if (motivesNotMet.Any())
			{
				var message = "No travellers decided to join due to lack of";

				foreach (var motiveNotMet in motivesNotMet) message += $" {motiveNotMet.Motive},";
				
				Model.EventLog.Alerts.Push(
					new EventLogModel.Entry(
						message.WrapColor("red"),
						Model.SimulationTime.Value
					)
				);
				return;
			}

			var spawn = Model.Buildings.AllActive
				.Where(m => m.Light.IsLightActive())
				.FirstOrDefault(m => m.Enterable.AnyAvailable());

			if (spawn == null)
			{
				Debug.LogError("Trying to spawn a dweller at the nearest fire, but could not find one with an available entrance");
				return;
			}

			var dweller = Model.Dwellers.Activate(
				spawn.RoomTransform.Id.Value,
				spawn.Enterable.Entrances.Value.First(e => e.State == Entrance.States.Available).Position
			);
			
			Model.EventLog.Dwellers.Push(
				new EventLogModel.Entry(
					$"The travelling visitor {dweller.Name.Value} decided to join your population".WrapColor("green"),
					Model.SimulationTime.Value,
					dweller.GetInstanceId()
				)
			);
		}
		#endregion
	}
}