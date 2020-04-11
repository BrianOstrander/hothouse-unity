using UnityEditor;
using UnityEngine;
using System;

namespace Lunra.Editor.Core
{
	public class TextDialogPopup : EditorWindow 
	{
		static Vector2 size = new Vector2(400f, 100f);

		static Vector2 CenterPosition => (new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)) + (size * 0.5f);

		public static class Styles
		{
			static GUIStyle descriptionLabel;

			public static GUIStyle DescriptionLabel
			{
				get 
				{
					if (descriptionLabel == null)
					{
						descriptionLabel = new GUIStyle(EditorStyles.label);
						descriptionLabel.alignment = TextAnchor.MiddleLeft;
						descriptionLabel.fontSize = 12;
						descriptionLabel.wordWrap = true;
					}
					return descriptionLabel;
				}
			}

			static GUIStyle textField;

			public static GUIStyle TextField
			{
				get 
				{
					if (textField == null)
					{
						textField = new GUIStyle(EditorStyles.textField);
						textField.alignment = TextAnchor.MiddleLeft;
						textField.fontSize = 16;
					}
					return textField;
				}
			}

			static GUIStyle button;

			public static GUIStyle Button
			{
				get
				{
					if (button == null)
					{
						button = new GUIStyle(EditorStyles.miniButton);
						button.alignment = TextAnchor.MiddleCenter;
						button.fixedWidth = 98f;
						button.fixedHeight = 32f;
						button.fontSize = 18;
					}

					return button;
				}
			}
		}

		Action<string> done;
		Action cancel;
		string doneText;
		string cancelText;
		string text;
		string description;
		bool closeHandled;
		bool lostFocusCloses;

		public static void Show (string title, Action<string> done, Action cancel = null, string doneText = "Okay", string cancelText = "Cancel", string text = "", string description = null, bool lostFocusCloses = true)
		{
			if (title == null) throw new ArgumentNullException(nameof(title));
			if (done == null) throw new ArgumentNullException(nameof(done));
			if (doneText == null) throw new ArgumentNullException(nameof(doneText));
			if (cancelText == null) throw new ArgumentNullException(nameof(cancelText));
			if (text == null) throw new ArgumentNullException(nameof(text));

			var window = EditorWindow.GetWindow(typeof (TextDialogPopup), true, title, true) as TextDialogPopup;
			// ReSharper disable once PossibleNullReferenceException
			window.done = done;
			window.cancel = cancel;
			window.doneText = doneText;
			window.cancelText = cancelText;
			window.text = text;
			window.description = description;
			window.lostFocusCloses = lostFocusCloses;

			window.position = new Rect(CenterPosition, size);

			window.Show();
		}

		void OnGUI () 
		{
			if (!string.IsNullOrEmpty(description)) GUILayout.Label(description, Styles.DescriptionLabel);

			text = GUILayout.TextField(text, Styles.TextField, GUILayout.ExpandWidth(true));

			GUILayout.FlexibleSpace();

			GUILayout.BeginHorizontal();
			{
				GUILayout.FlexibleSpace();
				if (GUILayout.Button(cancelText, Styles.Button)) 
				{
					cancel?.Invoke();
					closeHandled = true;
					Close();
				}
				if (GUILayout.Button(doneText, Styles.Button)) 
				{
					done?.Invoke(text);
					closeHandled = true;
					Close();
				}
			}
			GUILayout.EndHorizontal();
		}

		void OnDestroy()
		{
			if (!closeHandled) cancel?.Invoke();
		}

		void OnLostFocus()
		{
			if (lostFocusCloses)
			{
				cancel?.Invoke();
				closeHandled = true;
				Close();
			}
		}
	}
}