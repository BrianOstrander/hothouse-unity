using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using Lunra.StyxMvp.Models;
using Lunra.StyxMvp.Presenters;
using UnityEngine;

namespace Lunra.Hothouse.Presenters
{
	public class RadialCursorPresenter : Presenter<RadialCursorView>
	{
		GameModel game;
		ListenerProperty<Interaction.GenericVector3> interaction;

		public RadialCursorPresenter(
			GameModel game,
			ListenerProperty<Interaction.GenericVector3> interaction
		)
		{
			this.game = game;
			this.interaction = interaction;

			interaction.Changed += OnInteraction;
		}

		protected override void UnBind()
		{
			interaction.Changed -= OnInteraction;
		}
		
		#region InteractionModel Events
		void OnInteraction(Interaction.GenericVector3 interaction)
		{
			switch (interaction.State)
			{
				case Interaction.States.Idle:
				case Interaction.States.OutOfRange:
					break;
				case Interaction.States.Begin:
					View.Reset();
					View.Interaction(interaction.State, interaction.Value);
					ShowView(instant: true);
					break;
				case Interaction.States.Active:
					View.Interaction(interaction.State, interaction.Value);
					break;
				case Interaction.States.End:
				case Interaction.States.Cancel:
					View.Interaction(interaction.State, interaction.Value);
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