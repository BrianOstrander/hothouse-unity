using System;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using Lunra.StyxMvp.Presenters;
using UnityEngine;

namespace Lunra.Hothouse.Presenters
{
	public class GlobalInventoryCounterPresenter : Presenter<GlobalInventoryCounterView>
	{
		GameModel game;

		public GlobalInventoryCounterPresenter(GameModel game)
		{
			this.game = game;

			game.SimulationUpdate += OnGameSimulationUpdate;
			game.Cache.Changed += OnGameCache;

			game.Toolbar.IsEnabled.Changed += OnToolbarIsEnabled;
			
			Show();
		}

		protected override void Deconstruct()
		{
			game.SimulationUpdate -= OnGameSimulationUpdate;
			game.Cache.Changed -= OnGameCache;
			
			game.Toolbar.IsEnabled.Changed -= OnToolbarIsEnabled;
		}

		void Show()
		{
			if (View.Visible) return;
			
			View.Cleanup();

			View.Prepare += () => OnGameCache(game.Cache.Value);

			ShowView(instant: true);
		}
		
		#region GameModel Events
		void OnGameSimulationUpdate()
		{
			game.Items.Processor.Process(game.SimulationTimeDelta);
		}
		
		void OnGameCache(GameCache cache)
		{
			if (View.NotVisible) return;

			var result = string.Empty;

			result += game.SimulationTime.Value.ToString();
			
			result += "\nPopulation: " + cache.Population + "\n";

			Debug.LogWarning("TODO: Handle collecting of inventory available to player");
			// foreach (var type in EnumExtensions.GetValues(Inventory.Types.Unknown))
			// {
			// 	var color = "white";
			//
			// 	var count = cache.GlobalInventory.Available.Value[type];
			// 	var maximum = cache.GlobalInventory.AllCapacity.Value.GetMaximumFor(type);
			// 	
			// 	switch (type)
			// 	{
			// 		default:
			// 			if (count == 0) color = "yellow";
			// 			break;
			// 	}
			// 	
			// 	result += (type + ": " + count + " / " + maximum + "\n").Wrap("<color="+color+">", "</color>");
			// }
			
			void appendDiscontent(string title, float discontentNormal)
			{
				discontentNormal = 1f - discontentNormal;
				
				var color = "green";

				if (discontentNormal < 0.33f) color = "red";
				else if (discontentNormal < 0.66f) color = "yellow";

				result += $"\n<color={color}>{title}: {discontentNormal:N2}</color>";
			}

			if (cache.GoalsAverage.Values != null)
			{
				appendDiscontent("Total", cache.GoalsAverage.Total.DiscontentNormal);

				foreach (var goal in cache.GoalsAverage.Values)
				{
					appendDiscontent(goal.Motive.ToString(), goal.Value.DiscontentNormal);
				}
			}

			result += "\n";

			var dwellerEvents = game.EventLog.Dwellers.PeekAll()
				.Where(e => DayTime.Elapsed(game.SimulationTime.Value, e.SimulationTime).TotalTime < (DayTime.TimeInDay * 2f))
				.DistinctBy(e => e.Message)
				.ToList();
			
			for (var i = 0; i < Mathf.Min(10, dwellerEvents.Count); i++)
			{
				result += "\n - " + dwellerEvents[i];
			}

			var alertEvents = game.EventLog.Alerts.PeekAll()
				.Where(e => DayTime.Elapsed(game.SimulationTime.Value, e.SimulationTime).TotalTime < (DayTime.TimeInDay * 2f))
				.DistinctBy(e => e.Message)
				.ToList();

			if (alertEvents.Any())
			{
				result += "\n ------";

				for (var i = 0; i < Mathf.Min(10, alertEvents.Count); i++)
				{
					result += "\n - " + alertEvents[i];
				}
			}

			View.Label = result;
		}
		#endregion
		
		#region ToolbarModel Events
		void OnToolbarIsEnabled(bool isEnabled)
		{
			if (isEnabled) Show();
			else if (View.Visible) CloseView(true);
		}
		#endregion
	}
}