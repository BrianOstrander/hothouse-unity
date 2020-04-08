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

		static Vector2 Size = new Vector2(400f, 100f);

		static Vector2 CenterPosition
		{
			get
			{
				return (new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)) + (Size * 0.5f);
			}
		}

		static class Styles
		{
			static GUIStyle _DescriptionLabel;

			public static GUIStyle DescriptionLabel
			{
				get
				{
					if (_DescriptionLabel == null)
					{
						_DescriptionLabel = new GUIStyle(EditorStyles.label);
						_DescriptionLabel.alignment = TextAnchor.MiddleLeft;
						_DescriptionLabel.fontSize = 12;
						_DescriptionLabel.wordWrap = true;
					}
					return _DescriptionLabel;
				}
			}

			static GUIStyle _TextField;

			public static GUIStyle TextField
			{
				get
				{
					if (_TextField == null)
					{
						_TextField = new GUIStyle(EditorStyles.textField);
						_TextField.alignment = TextAnchor.MiddleLeft;
						_TextField.fontSize = 16;
					}
					return _TextField;
				}
			}

			static GUIStyle _Button;

			public static GUIStyle Button
			{
				get
				{
					if (_Button == null)
					{
						_Button = new GUIStyle(EditorStyles.miniButton);
						_Button.alignment = TextAnchor.MiddleCenter;
						_Button.fixedWidth = 98f;
						_Button.fixedHeight = 32f;
						_Button.fontSize = 18;
					}

					return _Button;
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
			if (title == null) throw new ArgumentNullException("title");
			if (entries == null) throw new ArgumentNullException("entries");
			if (cancelText == null) throw new ArgumentNullException("cancelText");

			var window = GetWindow(typeof(OptionPopupDialog), true, title, true) as OptionPopupDialog;
			window.entries = entries;
			window.cancel = cancel;
			window.cancelText = cancelText;
			window.description = description;
			window.lostFocusCloses = lostFocusCloses;

			window.position = new Rect(CenterPosition, new Vector2(Size.x, Size.y + (18f * entries.Length)));

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
					if (cancel != null) cancel();
					closeHandled = true;
					Close();
				}
			}
			GUILayout.EndHorizontal();
		}

		void OnDestroy()
		{
			if (!closeHandled && cancel != null) cancel();
		}

		void OnLostFocus()
		{
			if (lostFocusCloses)
			{
				if (cancel != null) cancel();
				closeHandled = true;
				Close();
			}
		}
	}
}