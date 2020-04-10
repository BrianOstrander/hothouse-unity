using System;

using UnityEditor;
using UnityEngine;

namespace LunraGamesEditor
{
	public class OptionPopupDialog : EditorWindow
	{
		public struct Entry
		{
			public static Entry Create(string text, Action done, string tooltip = null, Color? color = null)
			{
				return new Entry
				{
					Done = done,
					Content = new GUIContent(text, tooltip),
					Color = color
				};
			}

			public Action Done;
			public GUIContent Content;
			public Color? Color;
		}

		static Vector2 size = new Vector2(400f, 100f);

		static Vector2 CenterPosition => (new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)) + (size * 0.5f);

		static class Styles
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

		Entry[] entries;
		Action cancel;
		string cancelText;
		string description;
		bool lostFocusCloses;

		float optionScrollBar;
		bool closeHandled;

		public static void Show(
			string title,
			Entry[] entries,
			Action cancel = null,
			string cancelText = "Cancel",
			string description = null,
			bool lostFocusCloses = true
		)
		{
			if (title == null) throw new ArgumentNullException(nameof(title));
			if (entries == null) throw new ArgumentNullException(nameof(entries));
			if (cancelText == null) throw new ArgumentNullException(nameof(cancelText));

			var window = GetWindow(typeof(OptionPopupDialog), true, title, true) as OptionPopupDialog;
			// ReSharper disable once PossibleNullReferenceException
			window.entries = entries;
			window.cancel = cancel;
			window.cancelText = cancelText;
			window.description = description;
			window.lostFocusCloses = lostFocusCloses;

			window.position = new Rect(CenterPosition, new Vector2(size.x, size.y + (18f * entries.Length)));

			window.Show();
		}

		void OnGUI()
		{
			if (!string.IsNullOrEmpty(description)) GUILayout.Label(description, Styles.DescriptionLabel);

			optionScrollBar = GUILayout.BeginScrollView(new Vector2(0f, optionScrollBar), false, true).y;
			{
				foreach (var entry in entries)
				{
					if (entry.Color.HasValue) EditorGUILayoutExtensions.PushColor(entry.Color.Value);
					if (GUILayout.Button(entry.Content))
					{
						closeHandled = true;
						Close();
						entry.Done();
					}
					if (entry.Color.HasValue) EditorGUILayoutExtensions.PopColor();
				}
			}
			GUILayout.EndScrollView();

			GUILayout.BeginHorizontal();
			{
				GUILayout.FlexibleSpace();
				if (GUILayout.Button(cancelText, Styles.Button))
				{
					cancel?.Invoke();
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