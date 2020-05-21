using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using Lunra.StyxMvp.Presenters;

namespace Lunra.Hothouse.Presenters
{
	public class ToolbarPresenter : Presenter<ToolbarView>
	{
		GameModel game;
		ToolbarModel toolbar;
		
		public ToolbarPresenter(
			GameModel game
		)
		{
			this.game = game;
			toolbar = game.Toolbar;
			
			game.SimulationInitialize += OnGameSimulationInitialize;

			toolbar.IsEnabled.Changed += OnToolbarIsEnabled;
		}

		protected override void UnBind()
		{
			game.SimulationInitialize -= OnGameSimulationInitialize;
			
			toolbar.IsEnabled.Changed -= OnToolbarIsEnabled;
		}
		
		void Show()
		{
			if (View.Visible) return;
			
			View.Reset();

			View.GatherClick += OnGatherClick;
			View.BuildFireClick += OnBuildFireClick;
			View.BuildBedClick += OnBuildBedClick;
			
			ShowView(instant: true);
		}

		void Close()
		{
			if (View.NotVisible) return;
			
			CloseView(true);
		}
		
		#region GameModel Events
		void OnGameSimulationInitialize()
		{
			OnToolbarIsEnabled(toolbar.IsEnabled.Value);
		}
		#endregion
		
		#region ToolbarModel Events
		void OnToolbarIsEnabled(bool isEnabled)
		{
			if (isEnabled) Show();
			else Close();
		}
		#endregion
		
		#region GameInputModel Events
		// void OnGameInputFloor(Input)
		#endregion
		
		#region View Events
		void OnGatherClick()
		{
			// if (game.Interaction.Value.Type == gat)
			
			// game.Interaction.Value =
		}
		
		void OnBuildFireClick()
		{
			
		}
		
		void OnBuildBedClick()
		{
			
		}
		#endregion
		
		#region Utility
		/*
		void ToggleOrChangeInteraction(Interaction.Types type)
		{
			Assert.IsFalse(type == Interaction.Types.None, "It should not be possible to toggle to "+nameof(Interaction.Types.None));
			
			if (type == game.Interaction.Value.Type)
			{
				game.Interaction.Value = Interaction.None();
				return;
			}
			//
			// switch (type)
			// {
			// 	case Interaction.Types.AddClearance:
			// 		game.Interaction.Value = Interaction.AddClearanceIdle();
			// 		break;
			// 	case Interaction.Types.Construct:
			// 		break;
			// 	default:
			// 		Debug.LogError("Unrecognized Interaction.Type: "+type);
			// 		break;
			// }
		}
		*/
		#endregion
	}
}