using System.Collections.Generic;
using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.Satchel;
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
			
			// Do I need to calculate here??? Probably should have inventories do it when needed...
			// It becomes really hard to know what needs to be calculated when if we don't just calculate everything here
			foreach (var inventory in Model.Query.All<IInventoryModel>())
			{
				inventory.Inventory.Calculate();
			}
			
			// var dwellers = new List<DwellerModel>();
			//
			// foreach (var dweller in Model.Dwellers.AllActive)
			// {
			// 	if (dweller.InventoryPromises.All.None()) dwellers.Add(dweller);
			// }
			//
			// if (dwellers.None()) return;

			// var reservationInputs = new List<Item>();
			// var reservationOutputs = new List<Item>();
			//
			// foreach (var ())
		}
		#endregion
	}
}