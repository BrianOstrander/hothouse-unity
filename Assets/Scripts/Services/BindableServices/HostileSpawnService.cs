using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.NumberDemon;
using Lunra.StyxMvp.Services;

namespace Lunra.Hothouse.Services
{
	public class HostileSpawnService : BindableService<GameModel>
	{
		public HostileSpawnService(GameModel model) : base(model) { }

		Demon generator = new Demon();
		DayTime? nextSpawn;
		
		protected override void Bind()
		{
			IncrementNextSpawn();
			
			Model.SimulationUpdate += OnGameSimulationUpdate;
		}

		protected override void UnBind()
		{
			Model.SimulationUpdate -= OnGameSimulationUpdate;
		}
		
		// TODO: Don't hardcode this...
		void IncrementNextSpawn()
		{
			nextSpawn = Model.SimulationTime.Value + DayTime.FromRealSeconds(120f);
		}

		#region GameModel Events
		void OnGameSimulationUpdate()
		{
			if (Model.SimulationTime.Value < nextSpawn.Value) return;
			IncrementNextSpawn();

			var revealedRooms = Model.Rooms.AllActive.Where(m => !m.IsSpawn.Value && m.IsRevealed.Value).ToList();

			if ((revealedRooms.Count * 2) <= Model.SnapCaps.AllActive.Length) return;

			var availableGenerators = Model.Generators.AllActive
				.Where(m => m.LightSensitive.IsNotLit && m.Enterable.AnyAvailable())
				.Where(m => revealedRooms.Any(m.ShareRoom));

			if (availableGenerators.None()) return;

			var spawnLocation = generator.GetNextFrom(availableGenerators);
			
			Model.SnapCaps.Activate(
				spawnLocation.RoomTransform.Id.Value,
				spawnLocation.Enterable.Entrances.Value.First(e => e.IsNavigable).Position
			);
		}
		#endregion
	}
}