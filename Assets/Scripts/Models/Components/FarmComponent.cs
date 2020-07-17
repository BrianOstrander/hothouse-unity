using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Hothouse.Ai;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.AI;

namespace Lunra.Hothouse.Models
{
	public interface IFarmModel : IInventoryModel
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

					plot.State = FarmPlot.States.ReadyToPlow;
				}	
			}

			var plotRooms = plotsList
				.Select(p => p.RoomId)
				.Distinct();
			
			// foreach (var flora in game.Flora.)

			Plots = plotsList.ToArray();
		}
		
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