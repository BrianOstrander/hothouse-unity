using Lunra.Hothouse.Models;
using Lunra.Satchel;
using Lunra.StyxMvp.Services;

namespace Lunra.Hothouse.Services
{
	public class LogisticsService : BindableService<GameModel>
	{
		public LogisticsService(GameModel model) : base(model) { }
		
		protected override void Bind()
		{
			Model.SimulationUpdate += OnGameSimulationUpdate;
		}

		protected override void UnBind()
		{
			Model.SimulationUpdate -= OnGameSimulationUpdate;
		}
		
		#region GameModel Events
		void OnGameSimulationUpdate()
		{
			Model.Items.IterateAll(OnItemUpdate);	
		}

		void OnItemUpdate(Item item)
		{
			// if ()
		}
		#endregion
	}
}