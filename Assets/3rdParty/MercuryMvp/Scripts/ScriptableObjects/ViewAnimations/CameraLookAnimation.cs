using UnityEngine;
using LunraGames;

namespace LunraGames.SubLight
{
	public class CameraLookAnimation : ViewAnimation
	{
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		[SerializeField]
		bool horizontalOnly;
		[SerializeField]
		bool idleLook;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null

		Vector3 LookAtPosition(IView view)
		{
			var dir = App.V.CameraForward;

			return view.Root.position + (horizontalOnly ? dir.NewY(0).normalized : dir);
		}

		public override void OnShowing(IView view, float scalar)
		{
			view.Root.LookAt(LookAtPosition(view));
		}

		public override void OnLateIdle(IView view, float delta)
		{
			if (idleLook) view.Root.LookAt(LookAtPosition(view));
		}

		public override void OnClosing(IView view, float scalar)
		{
			view.Root.LookAt(LookAtPosition(view));
		}
	}
}