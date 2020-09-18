using Lunra.Hothouse.Models;
using Lunra.StyxMvp.Services;
using UnityEngine;

namespace Lunra.Hothouse.Services
{
	public class LogisticsService : BindableService<GameModel>
	{
		public LogisticsService(GameModel model) : base(model)
		{
			
		}
		
		protected override void Bind()
		{
			Model.SimulationUpdate += OnGameSimulationUpdate;
		}

		protected override void UnBind()
		{
			Model.SimulationUpdate -= OnGameSimulationUpdate;
		}

		bool hasBruk = false;
		
		#region GameModel Events
		void OnGameSimulationUpdate()
		{
			if (!hasBruk)
			{
				Debug.Break();
				hasBruk = true;
				return;
			}
			
			foreach (var inventory in Model.Query.All<IInventoryModel>())
			{
				inventory.Inventory.Calculate();
			}
		}
		#endregion
	}
}