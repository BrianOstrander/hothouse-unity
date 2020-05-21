using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using Lunra.StyxMvp.Presenters;
using UnityEngine;

namespace Lunra.Hothouse.Presenters
{
	public class RadialCursorPresenter : Presenter<RadialCursorView>
	{
		GameModel game;

		public RadialCursorPresenter(GameModel game)
		{
			this.game = game;

			game.Interaction.RadialFloorSelection.Changed += OnInteractionRadialFloorSelection;
		}

		protected override void UnBind()
		{
			game.Interaction.RadialFloorSelection.Changed -= OnInteractionRadialFloorSelection;
		}
		
		#region InteractionModel Events
		void OnInteractionRadialFloorSelection(Interaction.Generic interaction)
		{
			switch (interaction.State)
			{
				case Interaction.States.Idle: break;
				case Interaction.States.Begin:
					View.Reset();
					View.Interaction(interaction.State, interaction.Position);
					ShowView(instant: true);
					break;
				case Interaction.States.Active:
					View.Interaction(interaction.State, interaction.Position);
					break;
				case Interaction.States.End:
				case Interaction.States.Cancel:
					View.Interaction(interaction.State, interaction.Position);
					CloseView(true);
					break;
				default:
					Debug.LogError("Unrecognized Interaction.State: "+interaction.State);
					break;
			}
		}
		#endregion
	}
}