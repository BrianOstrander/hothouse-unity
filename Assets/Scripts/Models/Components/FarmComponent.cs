using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Ai;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public interface IFarmModel : IInventoryModel, IClaimOwnershipModel, IBoundaryModel, IEnterableModel
	{
		FarmComponent Farm { get; }
	}

	public class FarmComponent : ComponentModel<IFarmModel>
	{
		#region Serialized
		[JsonProperty] public bool IsFarm { get; private set; }
		[JsonProperty] public Vector2 Size { get; private set; }
		public string SelectedFloraType { get; set; } // todo make private or something...
		[JsonProperty] public FarmPlot[] Plots { get; private set; }
		#endregion
		
		#region Non Serialized 
		[JsonIgnore] public DateTime LastUpdatedRealTime { get; private set; }
		[JsonIgnore] public DayTime LastUpdated { get; private set; }

		public void SetLastUpdated(DayTime lastUpdated)
		{
			LastUpdatedRealTime = DateTime.Now;
			LastUpdated = lastUpdated;
		}
		#endregion

		public void CalculatePlots()



		{
			SetLastUpdated(Game.SimulationTime.Value);
			var plotsList = new List<FarmPlot>();

			if (string.IsNullOrEmpty(SelectedFloraType))
			{
				Plots = plotsList.ToArray();
				return;
			}
			
			var definition = Game.Flora.Definitions.First(d => d.Type == SelectedFloraType);

			var spacing = definition.ReproductionRadius.Maximum;
			var rowCount = Mathf.FloorToInt((Size.x / spacing));
			var columnCount = Mathf.FloorToInt((Size.y / spacing));

			if (rowCount <= 0) throw new Exception("Zero flora per row of this farm, this is unexpected");
			if (columnCount <= 0) throw new Exception("Zero flora per column of this farm, this is unexpected");

			var adjustedSize = new Vector3(Size.x - spacing, 0, Size.y - spacing);
			
			var origin = (Model.Transform.Rotation.Value * (adjustedSize * -0.5f)) + Model.Transform.Position.Value;
			var columnNormal = Model.Transform.Rotation.Value * Vector3.right;
			var rowNormal = Model.Transform.Rotation.Value * Vector3.forward;
			
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
					
					if (Vector3.Distance(plot.Position, Model.Transform.Position.Value) < Model.Boundary.Radius.Value) continue;
					
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
						Navigation.QueryEntrances(Model)
					);
					
					if (!isNavigable) continue;

					plot.State = FarmPlot.States.ReadyToSow;
				}	
			}

			Plots = plotsList.ToArray();

			CalculateFloraObligations();



		}
		
		public void CalculateFloraObligations()



		{
			SetLastUpdated(Game.SimulationTime.Value);
			
			var plotRooms = Plots
				.Select(p => p.RoomId)
				.Distinct();

			var blockedPlots = new List<string>();

			void tryAddDestroyObligation(FloraModel flora)
			{
				if (!flora.Obligations.HasAny(ObligationCategories.Destroy.Generic)) flora.Obligations.Add(ObligationCategories.Destroy.Generic);
			}
			
			foreach (var flora in Game.Flora.AllActive.Where(m => plotRooms.Contains(m.RoomTransform.Id.Value)))
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
				else if (flora.Farm.Value.Id == Model.Id.Value && flora.Age.Value.IsDone)
				{
					tryAddDestroyObligation(flora);
				}
			}

			foreach (var otherFarm in Game.Buildings.AllActive.Where(m => m.IsBuildingState(BuildingStates.Operating) && m.Farm.IsFarm))
			{
				if (otherFarm.Id.Value == Model.Id.Value) continue;
				if (!plotRooms.Contains(otherFarm.RoomTransform.Id.Value)) continue;
				
				foreach (var otherPlot in otherFarm.Farm.Plots.Where(p => p.State != FarmPlot.States.Invalid))
				{
					var nearbyPlots = Plots
						.Where(p => Vector3.Distance(p.Position, otherPlot.Position) < Mathf.Max(p.Radius.Minimum, otherPlot.Radius.Minimum));

					foreach (var nearbyPlot in nearbyPlots) nearbyPlot.State = FarmPlot.States.Invalid;
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
							plot.Flora = InstanceId.Null();
							plot.AttendingFarmer = InstanceId.Null();
						}
						break;
					case FarmPlot.States.Sown:
						if (!plot.Flora.TryGetInstance<FloraModel>(Game, out _))
						{
							plot.State = FarmPlot.States.ReadyToSow;
							plot.Flora = InstanceId.Null();
							plot.AttendingFarmer = InstanceId.Null();
						}
						break;
					case FarmPlot.States.Invalid:
					case FarmPlot.States.ReadyToSow:
						break;
					default:
						Debug.LogError("Unrecognized plot state: " + plot.State);
						break;
				}
				
				if (!plot.AttendingFarmer.IsNull && Model.Ownership.Claimers.Value.None(o => o.Id == plot.AttendingFarmer.Id))
				{
					plot.AttendingFarmer = InstanceId.Null();
				}
			}
		}

		public void RemoveAttendingFarmer(
			string attendingFarmerId
		)
		{
			foreach (var plot in Plots)
			{
				if (plot.AttendingFarmer.IsNull) continue;
				if (plot.AttendingFarmer.Id == attendingFarmerId) plot.AttendingFarmer = InstanceId.Null();
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
			Vector2 size,
			string selectedFloraType
		)
		{
			IsFarm = isFarm;
			Size = size;
			SelectedFloraType = selectedFloraType;
			Plots = new FarmPlot[0];
			
			SetLastUpdated(DayTime.Zero);
		}

		public override string ToString()
		{
			var result = "Farm: ";

			return result;
		}
	}
}