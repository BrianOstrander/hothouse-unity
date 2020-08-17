using System.Linq;
using Lunra.Hothouse.Models;
using Lunra.StyxMvp.Services;

namespace Lunra.Hothouse.Services
{
	public class HostileSpawnService : BindableService<GameModel>
	{
		public HostileSpawnService(GameModel model) : base(model) { }

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
			nextSpawn = Model.SimulationTime.Value + new DayTime(2);
		}

		#region GameModel Events
		void OnGameSimulationUpdate()
		{
			if (Model.SimulationTime.Value < nextSpawn.Value) return;
			IncrementNextSpawn();

			var revealedRooms = Model.Rooms.AllActive.Where(m => m.IsRevealed.Value).ToList();

			if (revealedRooms.Count <= Model.SnapCaps.AllActive.Length) return;
			
			// var availableWaterGenerators = Model.Decorations.AllActive
				// .Where(m => 0f < m.Flow.Value)
		}
		#endregion
	}
}