using UnityEngine;

namespace LunraGames.SubLight
{
	public class OpacityAnimation : ViewAnimation
	{
		[SerializeField]
		AnimationCurve showingOpacity = AnimationCurveExtensions.Constant(1f);
		[SerializeField]
		AnimationCurve closingOpacity = AnimationCurveExtensions.Constant(1f);

		public override void OnPrepare(IView view)
		{
			view.PushOpacity(OnQueryOpacity);
		}

		float OnQueryOpacity(IView view)
		{
			view.SetOpacityStale();
			switch (view.TransitionState)
			{
				case TransitionStates.Showing: return showingOpacity.Evaluate(view.ProgressScalar);
				case TransitionStates.Closing: return closingOpacity.Evaluate(view.ProgressScalar);
				case TransitionStates.Shown: return showingOpacity.Evaluate(1f);
				case TransitionStates.Closed: return closingOpacity.Evaluate(1f);
			}
			return 0f;
		}
	}
}