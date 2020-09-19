using System.Collections.Generic;
using System.Linq;
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
				// Debug.Break();
				hasBruk = true;
				return;
			}
			
			// Do I need to calculate here??? Probably should have inventories do it when needed...
			// It becomes really hard to know what needs to be calculated when if we don't just calculate everything here
			foreach (var inventory in Model.Query.All<IInventoryModel>())
			{
				inventory.Inventory.Calculate();
			}
			
			var dwellers = new Dictionary<string, DwellerModel>();
			
			foreach (var dweller in Model.Dwellers.AllActive)
			{
				if (dweller.InventoryPromises.All.None()) dwellers.Add(dweller.Id.Value, dweller);
			}
			
			if (dwellers.None()) return;

			var reservationInputs = new List<Item>();
			var reservationOutputs = new List<Item>();
			
			foreach (var item in Model.Items.All(i => i[Items.Keys.Shared.Type] == Items.Values.Shared.Types.Reservation))
			{
				var logisticState = item[Items.Keys.Reservation.LogisticState];
				
				if (logisticState == Items.Values.Reservation.LogisticStates.Input) reservationInputs.Add(item);
				else if (logisticState == Items.Values.Reservation.LogisticStates.Output) reservationOutputs.Add(item);
				else Debug.LogError($"Unrecognized {Items.Keys.Reservation.LogisticState} on reservation {item}");
			}

			// while (dwellers.Any() && reservationOutputs.Any())
			// {
			// 	
			// }
		}
		#endregion
	}
}