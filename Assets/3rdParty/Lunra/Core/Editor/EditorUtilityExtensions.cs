using UnityEngine;
using UnityEditor;
using Lunra.Core;
using System;

namespace Lunra.Editor.Core
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
					onOkay?.Invoke();
					break;
				case 1:
					onCancel?.Invoke();
					break;
				case 2:
					onAlt?.Invoke();
					break;
			}
		}
	}
}