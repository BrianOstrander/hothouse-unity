using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Ai;
using Lunra.Hothouse.Models;
using Lunra.Satchel;
using Lunra.StyxMvp.Services;
using UnityEngine;

namespace Lunra.Hothouse.Services
{
	public class LogisticsService : BindableService<GameModel>
	{
		struct ReservationCache
		{
			public Item Item;
			public Vector3 Position;
			public int Priority;
			public IInventoryModel Parent;
			public Dictionary<long, bool> NavigationCache;
		}
		
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

			var reservationInputs = new Dictionary<long, ReservationCache>();
			var reservationOutputs = new Dictionary<long, ReservationCache>();

			foreach (var item in Model.Items.All(i => i[Items.Keys.Shared.Type] == Items.Values.Shared.Types.Reservation))
			{
				var logisticState = item[Items.Keys.Reservation.LogisticState];

				var cache = new ReservationCache
				{
					Item = item,
					NavigationCache = new Dictionary<long, bool>()
				};

				if (Model.Query.TryFindFirst(m => m.Inventory.Container.Id == item.ContainerId, out cache.Parent))
				{
					cache.Position = cache.Parent.Transform.Position.Value;
					
					if (logisticState == Items.Values.Reservation.LogisticStates.Input) reservationInputs.Add(item.Id, cache);
					else if (logisticState == Items.Values.Reservation.LogisticStates.Output) reservationOutputs.Add(item.Id, cache);
					else Debug.LogError($"Unrecognized {Items.Keys.Reservation.LogisticState} on reservation {item}");
				}
				else Debug.LogError($"Cannot find parent of container {item.ContainerId} for item {item}");
			}

			// var navigationCache = new Dictionary<long, Dictionary<long, bool>>();

			var reservationInputsSorted = reservationInputs.Values
				.OrderBy(r => r.Priority)
				.ToList();
			
			while (dwellers.Any() && reservationInputs.Any())
			{
				var reservationInput = reservationInputsSorted[0];
				reservationInputsSorted.RemoveAt(0);

				if (!reservationInput.Parent.Inventory.Capacities.TryGetValue(reservationInput.Item[Items.Keys.Reservation.CapacityId], out var filter))
				{
					Debug.LogError($"Cannot find filter for reservation {reservationInput.Item}");
					continue;
				}

				var reservationInputRemaining = reservationInput.Item.InstanceCount;
				
				var reservationOutputsSorted = reservationOutputs.Values
					.OrderBy(r => r.Priority)
					.ThenBy(r => r.Parent.DistanceTo(reservationInput.Parent))
					.ToList();

				while (0 < reservationInputRemaining && reservationOutputsSorted.Any() && dwellers.Any())
				{
					var reservationOutput = reservationOutputsSorted[0];
					reservationOutputsSorted.RemoveAt(0);

					var reservationOutputRemaining = reservationOutput.Item.InstanceCount;
					
					var dwellersSorted = dwellers.Values
						.OrderBy(d => d.DistanceTo(reservationOutput.Parent))
						.ToList();

					while (0 < reservationOutputRemaining && 0 < reservationInputRemaining && dwellersSorted.Any())
					{
						var dweller = dwellersSorted[0];
						dwellersSorted.RemoveAt(0);

						if (!Navigation.TryQuery(reservationInput.Parent, out var queryInput))
						{
							Debug.LogError($"Unable to query {reservationInput.Parent}");
							continue;
						}

						var isNavigableToInput = NavigationUtility.CalculateNearest(
							dweller.Transform.Position.Value,
							out _,
							queryInput
						);
						
						if (!isNavigableToInput) continue;

						if (!Navigation.TryQuery(reservationOutput.Parent, out var queryOutput))
						{
							Debug.LogError($"Unable to query {reservationOutput.Parent}");
						}
						
						var isNavigableToOutput = NavigationUtility.CalculateNearest(
							dweller.Transform.Position.Value,
							out _,
							queryOutput
						);
						
						if (!isNavigableToOutput) continue;
						
						var items = reservationOutput.Parent.Inventory.Container
							.All(i => i[Items.Keys.Resource.CapacityPoolId] == reservationOutput.Item[Items.Keys.Reservation.CapacityPoolId])
							.ToList();

						foreach (var (item, stack) in items)
						{
							if (filter.Validate(item))
							{
								reservationInputRemaining--;
								reservationOutputRemaining--;
								
								var source = new InventoryPromiseComponent.TransferInfo
								{
									Container = reservationOutput.Parent.Inventory.Container,
									Capacity = Model.Items.First(reservationOutput.Item[Items.Keys.Reservation.CapacityId]),
									
									// It's okay if the source doesn't have a capacity pool.
									CapacityPool = Model.Items.FirstOrDefault(reservationOutput.Item[Items.Keys.Reservation.CapacityPoolId]),
									
									Reservation = reservationOutput.Item
								};

								var destination = new InventoryPromiseComponent.TransferInfo
								{
									Container = reservationInput.Parent.Inventory.Container,
									Capacity = Model.Items.First(reservationInput.Item[Items.Keys.Reservation.CapacityId]),
									
									// This capacity pool is required.
									CapacityPool = Model.Items.First(reservationInput.Item[Items.Keys.Reservation.CapacityPoolId]),
									
									Reservation = reservationInput.Item,
								};
								
								var isReservationInputSatisfied = dweller.InventoryPromises.Transfer(
									item,
									source,
									destination
								);

								dwellers.Remove(dweller.Id.Value);
						
								if (isReservationInputSatisfied) break;
							}
						}	
					}
				}

				// var dwellersSorted = dwellers.Values
				// 	.OrderBy(d => d.DistanceTo(reservationInput.Parent))
				// 	.ToList();
				//
				// while (dwellersSorted.Any())
				// {
				// 	var dweller = dwellersSorted[0];
				// 	dwellersSorted.RemoveAt(0);
				// 	
				// 	
				// }
			}
		}
		#endregion
	}
}