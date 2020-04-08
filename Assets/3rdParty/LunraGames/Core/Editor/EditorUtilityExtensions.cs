using UnityEngine;
using UnityEditor;
using LunraGames;
using System;

namespace LunraGamesEditor
{
	public static class EditorUtilityExtensions
	{
		public static bool GetWindowVisible(Type type)
		{
			var windows = Resources.FindObjectsOfTypeAll(type);
			return windows != null && 0 < windows.Length;
		}

		public static bool GetWindowVisible<T>()
		{
			return GetWindowVisible(typeof(T));
		}

		public static EditorWindow GetGameWindow()
		{
			var type = Type.GetType("UnityEditor.GameView,UnityEditor");
			return GetWindowVisible(type) ? EditorWindow.GetWindow(type) : null;
		}

		public static bool DialogInvalid(string message = null)
		{
			return Dialog(Strings.Dialogs.Titles.Invalid, message);
		}

		public static bool DialogError(string message = null)
		{
			return Dialog(Strings.Dialogs.Titles.Error, message);
		}

		static bool Dialog(string title, string message)
		{
			title = StringExtensions.IsNullOrWhiteSpace(title) ? Strings.Dialogs.Titles.Alert : title;
			message = StringExtensions.IsNullOrWhiteSpace(message) ? Strings.Dialogs.Messages.InvalidOperation : message;
			return EditorUtility.DisplayDialog(title, message, Strings.Dialogs.Responses.Okay);
		}

		public static void YesNoCancelDialog(string message, Action yes = null, Action no = null, Action cancel = null)
		{
			YesNoCancelDialog(null, message, yes, no, cancel);
		}

		public static void YesNoCancelDialog(string title, string message, Action yes = null, Action no = null, Action cancel = null)
		{
			ComplexDialog(title, message, Strings.Dialogs.Responses.Yes, Strings.Dialogs.Responses.Cancel, Strings.Dialogs.Responses.No, yes, cancel, no);
		}

		static void ComplexDialog(string title, string message, string okay, string cancel, string alt, Action onOkay, Action onCancel, Action onAlt)
		{
			title = StringExtensions.IsNullOrWhiteSpace(title) ? Strings.Dialogs.Titles.Alert : title;
			message = StringExtensions.IsNullOrWhiteSpace(message) ? Strings.Dialogs.Messages.InvalidOperation : message;
			okay = StringExtensions.IsNullOrWhiteSpace(okay) ? Strings.Dialogs.Responses.Okay : okay;
			cancel = StringExtensions.IsNullOrWhiteSpace(cancel) ? Strings.Dialogs.Responses.Cancel : cancel;
			alt = StringExtensions.IsNullOrWhiteSpace(alt) ? Strings.Dialogs.Responses.Other : alt;
			var result = EditorUtility.DisplayDialogComplex(title, message, okay, cancel, alt);

			switch (result)
			{
				case 0: 
					if (onOkay != null) onOkay(); 
					break;
				case 1:
					if (onCancel != null) onCancel();
					break;
				case 2:
					if (onAlt != null) onAlt();
					break;
			}
		}

		public static Color ColorFromString(string value)
		{
			return Color.cyan.NewH(int.Parse(CreateMD5(value).Substring(0, 2), System.Globalization.NumberStyles.HexNumber) / 255f);
		}

		public static Color ColorFromIndex(int value)
		{
			return Color.cyan.NewH((value * 0.618034f) % 1f);
		}

		// TODO: Lol hacks.
		static string CreateMD5(string input)
		{
			input = input ?? string.Empty;
			// Use input string to calculate MD5 hash
			using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
			{
				var inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
				var hashBytes = md5.ComputeHash(inputBytes);

				// Convert the byte array to hexadecimal string
				var sb = new System.Text.StringBuilder();
				for (int i = 0; i < hashBytes.Length; i++)
				{
					sb.Append(hashBytes[i].ToString("X2"));
				}
				return sb.ToString();
			}
		}
	}
}