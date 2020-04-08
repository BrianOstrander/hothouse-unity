using UnityEditor;
using UnityEngine;
using System;

namespace LunraGamesEditor
{
	public class TextDialogPopup : EditorWindow 
	{
		static Vector2 Size = new Vector2(400f, 100f);

		static Vector2 CenterPosition
		{
			get
			{
				return (new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)) + (Size * 0.5f);
			}
		}

		public static class Styles
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

		Action<string> Done;
		Action Cancel;
		string DoneText;
		string CancelText;
		string Text;
		string Description;
		bool CloseHandled;
		bool LostFocusCloses;

		public static void Show (string title, Action<string> done, Action cancel = null, string doneText = "Okay", string cancelText = "Cancel", string text = "", string description = null, bool lostFocusCloses = true)
		{
			if (title == null) throw new ArgumentNullException("title");
			if (done == null) throw new ArgumentNullException("done");
			if (doneText == null) throw new ArgumentNullException("doneText");
			if (cancelText == null) throw new ArgumentNullException("cancelText");
			if (text == null) throw new ArgumentNullException("text");

			var window = EditorWindow.GetWindow(typeof (TextDialogPopup), true, title, true) as TextDialogPopup;
			window.Done = done;
			window.Cancel = cancel;
			window.DoneText = doneText;
			window.CancelText = cancelText;
			window.Text = text;
			window.Description = description;
			window.LostFocusCloses = lostFocusCloses;

			window.position = new Rect(CenterPosition, Size);

			window.Show();
		}

		void OnGUI () 
		{
			if (!string.IsNullOrEmpty(Description)) GUILayout.Label(Description, Styles.DescriptionLabel);

			Text = GUILayout.TextField(Text, Styles.TextField, GUILayout.ExpandWidth(true));

			GUILayout.FlexibleSpace();

			GUILayout.BeginHorizontal();
			{
				GUILayout.FlexibleSpace();
				if (GUILayout.Button(CancelText, Styles.Button)) 
				{
					if (Cancel != null) Cancel();
					CloseHandled = true;
					Close();
				}
				if (GUILayout.Button(DoneText, Styles.Button)) 
				{
					if (Done != null) Done(Text); 
					CloseHandled = true;
					Close();
				}
			}
			GUILayout.EndHorizontal();
		}

		void OnDestroy()
		{
			if (!CloseHandled && Cancel != null) Cancel();
		}

		void OnLostFocus()
		{
			if (LostFocusCloses)
			{
				if (Cancel != null) Cancel();
				CloseHandled = true;
				Close();
			}
		}
	}
}