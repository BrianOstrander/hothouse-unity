using System;

using UnityEditor;
using UnityEngine;

namespace LunraGamesEditor
{
	public class FlexiblePopupDialog : EditorWindow
	{
		Action<Action> onGui;
		Action onClose;
		Action onLostFocus;
		bool lostFocusCloses;

		bool onCloseOrLostFocusCalled;

		public static void Show(
			string title,
			Vector2 size,
			Action<Action> onGui,
			Action onClose = null,
			Action onLostFocus = null,
			bool lostFocusCloses = true
		)
		{
			if (title == null) throw new ArgumentNullException("title");
			if (onGui == null) throw new ArgumentNullException("onGui");

			var window = GetWindow(typeof(FlexiblePopupDialog), true, title, true) as FlexiblePopupDialog;

			//window.size = size;
			window.onGui = onGui;

			window.onClose = onClose;
			window.onLostFocus = onLostFocus;
			window.lostFocusCloses = lostFocusCloses;

			window.position = EditorGUIExtensions.GetPositionOnScreen(size);

			window.Show();
		}

		void OnGUI()
		{
			onGui(Close);
		}

		void OnDestroy()
		{
			if (onClose != null) onClose();
		}

		void OnLostFocus()
		{
			if (onLostFocus != null) onLostFocus();
			if (lostFocusCloses) Close();
		}
	}
}