using System;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using Lunra.StyxMvp.Presenters;

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
			
			View.Reset();

			View.Prepare += () => OnGameCache(game.Cache.Value);

			ShowView(instant: true);
		}
		
		#region GameModel Events
		void OnGameCache(GameCache cache)
		{
			if (View.NotVisible) return;

			var result = string.Empty;

			foreach (var type in EnumExtensions.GetValues(Inventory.Types.Unknown))
			{
				var color = "white";

				var count = cache.GlobalInventory[type];
				var maximum = cache.GlobalInventoryCapacity.GetMaximumFor(type);
				
				switch (type)
				{
					case Inventory.Types.Stalks:
					case Inventory.Types.Scrap:
						if (count == 0) color = "yellow";
						break;
					case Inventory.Types.Rations:
						if (cache.Conditions.TryGetValue(Condition.Types.NoRations, out var noRations) && noRations) color = "red";
						else if (cache.Conditions.TryGetValue(Condition.Types.LowRations, out var lowRations) && lowRations) color = "yellow";
						break;
					default: throw new ArgumentOutOfRangeException();
				}
				
				result += StringExtensions.Wrap(type + ": " + count + " / " + maximum + "\n", "<color="+color+">", "</color>");
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