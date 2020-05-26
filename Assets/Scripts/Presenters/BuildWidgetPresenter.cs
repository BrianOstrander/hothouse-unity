using System.Linq;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using Lunra.StyxMvp.Models;
using Lunra.StyxMvp.Presenters;
using UnityEngine;

namespace Lunra.Hothouse.Presenters
{
	public class BuildWidgetPresenter : Presenter<BuildWidgetView>
	{
		GameModel game;

		public BuildWidgetPresenter(GameModel game)
		{
			this.game = game;

			game.Toolbar.Task.Changed += OnToolbarTask;
			game.Toolbar.IsEnabled.Changed += OnToolbarIsEnabled;
			game.Interaction.FloorSelection.Changed += OnInteractionFloorSelection;
		}

		protected override void UnBind()
		{
			game.Toolbar.Task.Changed -= OnToolbarTask;
			game.Toolbar.IsEnabled.Changed -= OnToolbarIsEnabled;
			game.Interaction.FloorSelection.Changed -= OnInteractionFloorSelection;
		}

		void Show()
		{
			if (View.Visible) return;
			
			View.Reset();

			ShowView(instant: true);
			
			
		}

		void Close()
		{
			if (View.NotVisible) return;
			
			CloseView(true);
		}
		
		#region InteractionModel Events
		void OnInteractionFloorSelection(Interaction.Generic interaction)
		{
			if (View.NotVisible) return;

			var lightValue = game.CalculateMaximumLighting((game.Rooms.AllActive.First().Id.Value, interaction.Position.Current));

			View.IsInvalid = Mathf.Approximately(0f, lightValue);

			View.RootTransform.position = interaction.Position.Current;
		}
		#endregion
		
		#region ToolbarModel Events
		void OnToolbarTask(ToolbarModel.Tasks task)
		{
			if (task == ToolbarModel.Tasks.Construction) Show();
			else Close();
		}

		void OnToolbarIsEnabled(bool isEnabled)
		{
			OnToolbarTask(game.Toolbar.Task.Value);
		}
		#endregion
	}
}