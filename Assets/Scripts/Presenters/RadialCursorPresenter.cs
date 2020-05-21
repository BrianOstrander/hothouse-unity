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

			game.Interaction.Floor.Changed += OnInteractionFloor;
		}

		protected override void UnBind()
		{
			game.Interaction.Floor.Changed += OnInteractionFloor;
		}
		
		#region InteractionModel Events
		void OnInteractionFloor(Interaction.Generic interaction)
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