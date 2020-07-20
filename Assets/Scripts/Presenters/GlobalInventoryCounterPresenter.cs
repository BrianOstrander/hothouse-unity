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

			game.Cache.Changed += OnGameCache;

			game.Toolbar.IsEnabled.Changed += OnToolbarIsEnabled;
			
			Show();
		}

		protected override void UnBind()
		{
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
		void OnGameCache(GameCache cache)
		{
			if (View.NotVisible) return;

			var result = string.Empty;

			result += "Population: " + cache.Population + "\n";

			foreach (var type in EnumExtensions.GetValues(Inventory.Types.Unknown))
			{
				var color = "white";

				var count = cache.GlobalInventory.Available.Value[type];
				var maximum = cache.GlobalInventory.AllCapacity.Value.GetMaximumFor(type);
				
				switch (type)
				{
					case Inventory.Types.Rations:
						if (cache.Conditions.TryGetValue(Condition.Types.NoRations, out var noRations) && noRations) color = "red";
						else if (cache.Conditions.TryGetValue(Condition.Types.LowRations, out var lowRations) && lowRations) color = "yellow";
						break;
					default:
						if (count == 0) color = "yellow";
						break;
				}
				
				result += StringExtensions.Wrap(type + ": " + count + " / " + maximum + "\n", "<color="+color+">", "</color>");
			}

			result += "\n";

			var dwellerEvents = game.EventLog.DwellerEntries.PeekAll()
				.Where(e => DayTime.Elapsed(game.SimulationTime.Value, e.SimulationTime).TotalTime < (DayTime.TimeInDay * 2f))
				.ToList();
			
			for (var i = 0; i < Mathf.Min(10, dwellerEvents.Count); i++)
			{
				result += "\n - " + dwellerEvents[i];
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