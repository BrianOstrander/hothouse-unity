using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Ai;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.AI;

namespace Lunra.Hothouse.Models
{
	public interface IFarmModel : IInventoryModel, IClaimOwnershipModel
	{
		FarmComponent Farm { get; }
	}

	public class FarmComponent : Model
	{
		#region Serialized
		public bool IsFarm { get; private set; }
		public Vector2 Size { get; private set; }
		public Inventory.Types SelectedSeed { get; set; } // todo make private or something...
		public FarmPlot[] Plots { get; private set; }
		#endregion
		
		#region Non Serialized 
		[JsonIgnore] public DateTime LastUpdated { get; private set; } 
		#endregion

		public FarmComponent()
		{
			
		}

		public void CalculatePlots(
			GameModel game,
			IFarmModel model
		)
		{
			LastUpdated = DateTime.Now;
			var plotsList = new List<FarmPlot>();

			if (SelectedSeed == Inventory.Types.Unknown)
			{
				Plots = plotsList.ToArray();
				return;
			}
			
			model.Inventory.Desired.Value = InventoryDesire.UnCalculated(
				Inventory.FromEntry(
					SelectedSeed,
					model.Inventory.AllCapacity.Value.GetMaximumFor(SelectedSeed)
				)
			);

			var definition = game.Flora.Definitions.First(d => d.Seed == SelectedSeed);

			var spacing = definition.ReproductionRadius.Minimum;
			var rowCount = Mathf.FloorToInt((Size.x / spacing));
			var columnCount = Mathf.FloorToInt((Size.y / spacing));

			if (rowCount <= 0) throw new Exception("Zero flora per row of this farm, this is unexpected");
			if (columnCount <= 0) throw new Exception("Zero flora per column of this farm, this is unexpected");

			var adjustedSize = new Vector3(Size.x - spacing, 0, Size.y - spacing);
			
			var origin = (model.Transform.Rotation.Value * (adjustedSize * -0.5f)) + model.Transform.Position.Value;
			var columnNormal = model.Transform.Rotation.Value * Vector3.right;
			var rowNormal = model.Transform.Rotation.Value * Vector3.forward;
			
			for (var y = 0f; y < columnCount; y++)
			{
				var rowOrigin = origin + (rowNormal * (y * spacing));
				for (var x = 0f; x < rowCount; x++)
				{
					var plot = new FarmPlot(
						rowOrigin + (columnNormal * (x * spacing)),
						null,
						definition.ReproductionRadius,
						FarmPlot.States.Invalid,
						InstanceId.Null()
					);

					plotsList.Add(plot);
					
					var isNavigable = NavigationUtility.CalculateNearestFloor(
							plot.Position,
							out var navHit,
							out _,
							out var roomId
					);
					
					if (!isNavigable) continue;
					if (string.IsNullOrEmpty(roomId)) continue;

					plot.Position = navHit.position;
					plot.RoomId = roomId;
					
					isNavigable = NavigationUtility.CalculateNearest(
						plot.Position,
						out _,
						Navigation.QueryEntrances(model)
					);
					
					if (!isNavigable) continue;

					plot.State = FarmPlot.States.ReadyToSow;
				}	
			}

			Plots = plotsList.ToArray();
			
			CalculateFloraObligations(
				game,
				model
			);
		}
		
		public void CalculateFloraObligations(
			GameModel game,
			IFarmModel model
		)
		{
			LastUpdated = DateTime.Now;
			
			var plotRooms = Plots
				.Select(p => p.RoomId)
				.Distinct();

			var blockedPlots = new List<string>();

			void tryAddDestroyObligation(FloraModel flora)
			{
				if (!flora.Obligations.HasAny(ObligationCategories.Destroy.Melee)) flora.Obligations.Add(ObligationCategories.Destroy.Melee);
			}
			
			foreach (var flora in game.Flora.AllActive.Where(m => plotRooms.Contains(m.RoomTransform.Id.Value)))
			{
				if (flora.Farm.Value.IsNull)
				{
					var invalidPlots = Plots
						.Where(p => Vector3.Distance(p.Position, flora.Transform.Position.Value) < p.Radius.Maximum);
					
					if (invalidPlots.Any())
					{
						tryAddDestroyObligation(flora);
						
						foreach (var blockedPlot in invalidPlots.Where(p => p.State == FarmPlot.States.ReadyToSow))
						{
							blockedPlot.State = FarmPlot.States.Blocked;
							blockedPlots.Add(blockedPlot.Id);
						}
					}
				}
				else if (flora.Farm.Value.Id == model.Id.Value && flora.Age.Value.IsDone)
				{
					tryAddDestroyObligation(flora);
				}
			}

			foreach (var plot in Plots)
			{
				switch (plot.State)
				{
					case FarmPlot.States.Blocked:
						if (!blockedPlots.Contains(plot.Id))
						{
							plot.State = FarmPlot.States.ReadyToSow;
						}
						break;
					case FarmPlot.States.Sown:
						if (!plot.Flora.TryGetInstance<FloraModel>(game, out _))
						{
							plot.State = FarmPlot.States.ReadyToSow;
						}
						break;
					case FarmPlot.States.Invalid:
					case FarmPlot.States.ReadyToSow:
						break;
					default:
						Debug.LogError("Unrecognized plot state: " + plot.State);
						break;
				}
				
				if (!plot.AttendingFarmer.IsNull && model.Ownership.Claimers.Value.None(o => o.Id == plot.AttendingFarmer.Id))
				{
					plot.AttendingFarmer = InstanceId.Null();
				}
			}
		}

		// public bool CanSow(
		// 	IFarmModel model,
		// 	AgentModel agent,
		// 	out FarmPlot plot
		// )
		// {
		// 	plot = Plots
		// 		.Where(p => p.State == FarmPlot.States.ReadyToSow)
		// 		.OrderBy(p => Vector3.Distance(agent.Transform.Position.Value, p.Position))
		// 		.FirstOrDefault();
		//
		// 	if (plot == null) return false;
		// 	if (!model.Inventory.Available.Value.Contains(Inventory.FromEntry(SelectedSeed, 1))) return false;
		// 	
		// 	return Vector3.Distance(agent.Transform.Position.Value, plot.Position) < plot.Radius;
		// }
		
		public void Reset(
			bool isFarm,
			Vector2 size	
		)
		{
			IsFarm = isFarm;
			Size = size;
			SelectedSeed = Inventory.Types.Unknown;
			Plots = new FarmPlot[0];
			
			LastUpdated = DateTime.MinValue;
		}

		public override string ToString()
		{
			var result = "Farm: ";

			return result;
		}
	}
}