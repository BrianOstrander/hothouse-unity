using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using Lunra.StyxMvp.Presenters;
using UnityEngine;

namespace Lunra.Hothouse.Presenters
{
	public class HintsPresenter : GenericPresenter<TestView>
	{
		GameModel game;
		HintsModel hints;

		public HintsPresenter(GameModel game)
		{
			this.game = game;
			hints = game.Hints;

			game.SimulationUpdate += OnGameSimulationUpdate;
		}

		protected override void UnBind()
		{
			game.SimulationUpdate -= OnGameSimulationUpdate;
		}
		
		#region GameModel Events
		void OnGameSimulationUpdate()
		{
			var instance = hints.HintCollections.Value;
			var hasDelta = false;
			for (var collectionIndex = 0; collectionIndex < instance.Length; collectionIndex++)
			{
				var hintCollection = instance[collectionIndex];
				switch (hintCollection.State)
				{
					case HintStates.Idle:
						hintCollection.State = HintStates.Active;
						hasDelta = true;
						break;
					case HintStates.Dismissed:
						continue;
				}

				var allDismissed = true;
				for (var hintIndex = 0; hintIndex < hintCollection.Hints.Length; hintIndex++)
				{
					var beginState = hintCollection.Hints[hintIndex].State;
					if (hintCollection.Hints[hintIndex].Evaluate(game, out var hintDelta))
					{
						hintCollection.Hints[hintIndex] = hintDelta;
						hasDelta = true;
						UpdateHint(hintDelta);
					}

					allDismissed &= hintDelta.State == HintStates.Dismissed;
				}

				if (allDismissed) hintCollection.State = HintStates.Dismissed;

				instance[collectionIndex] = hintCollection;
				break;
			}

			if (hasDelta) hints.HintCollections.Value = instance;
		}
		#endregion

		void UpdateHint(Hint hint)
		{
			switch (hint.State)
			{
				case HintStates.Idle:
					Debug.LogWarning("It should not be possible to change a hint back to idle...");
					break;
				case HintStates.Active:
					Debug.Log(StringExtensions.Wrap("Hint Active: "+hint.Message, "<color=green>", "</color>"));
					break;
				case HintStates.Dismissed:
					Debug.Log(StringExtensions.Wrap("Hint Dismiss: "+hint.Message, "<color=red>", "</color>"));
					break;
				default:
					Debug.LogError("Unrecognized "+nameof(hint.State)+": "+hint.State);
					break;
			}
		}
		
		#region View Events
		#endregion
	}
}