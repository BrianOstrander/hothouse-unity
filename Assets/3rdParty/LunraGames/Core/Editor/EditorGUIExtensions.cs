using System.Collections.Generic;

using UnityEngine;

using UnityEditor;

namespace LunraGamesEditor
{
	// ReSharper disable once InconsistentNaming
	public static class EditorGUIExtensions
	{
		enum ChangeCheckStates
		{
			Listening,
			Paused
		}

		static int currentLevel;
		static Stack<ChangeCheckStates> changeCheckStateStack = new Stack<ChangeCheckStates>();
		static Stack<bool> changeValueStack = new Stack<bool>();

		static bool IsActive => 0 < currentLevel;

		public static void BeginChangeCheck()
		{
			if (IsActive)
			{
				if (changeCheckStateStack.Peek() == ChangeCheckStates.Paused)
				{
					Debug.LogError("Cannot begin a change check in the middle of a pause.");
					return;
				}

				// If anything messes up, it's probably because of the next two lines...
				var previous = false;
				try { previous = EditorGUI.EndChangeCheck(); } catch {}

				previous = changeValueStack.Pop() || previous;
				changeValueStack.Push(previous);
			}

			changeValueStack.Push(false);

			changeCheckStateStack.Push(ChangeCheckStates.Listening);
			currentLevel++;

			EditorGUI.BeginChangeCheck();
		}

		public static bool EndChangeCheck(ref bool changed)
		{
			changed = EndChangeCheck() || changed;
			return changed;
		}

		public static bool EndChangeCheck()
		{
			if (!IsActive)
			{
				Debug.LogError("Cannot end a change check that has not been started.");
				return false;
			}
			if (changeCheckStateStack.Peek() == ChangeCheckStates.Paused)
			{
				Debug.LogError("Cannot end a change check that has been paused.");
				return false;
			}

			changeCheckStateStack.Pop();
			currentLevel--;
			var result = changeValueStack.Pop();
			result = EditorGUI.EndChangeCheck() || result;
			return result;
		}

		public static void PauseChangeCheck()
		{
			if (!IsActive) return;
			if (changeCheckStateStack.Peek() == ChangeCheckStates.Paused) return;

			changeCheckStateStack.Push(ChangeCheckStates.Paused);

			var current = EditorGUI.EndChangeCheck();
			current = changeValueStack.Pop() || current;
			changeValueStack.Push(current);
		}

		public static void UnPauseChangeCheck()
		{
			if (!IsActive) return;
			if (changeCheckStateStack.Peek() != ChangeCheckStates.Paused)
			{
				Debug.LogError("Cannot unpause a change check that has not been paused.");
				return;
			}

			changeCheckStateStack.Pop();

			EditorGUI.BeginChangeCheck();
		}

		/* Not possible it seems...
		public static bool IsOnScreen(Rect position)
		{
			var screenRect = new Rect(0f, 0f, Screen.currentResolution.width, Screen.currentResolution.height);
			return screenRect.Contains(position.min) && screenRect.Contains(position.max);
		}
		*/

		/// <summary>
		/// Gets the position on screen either around the cursor, or centered, depending on the size of the window.
		/// </summary>
		/// <returns>The position on screen.</returns>
		/// <param name="size">Size.</param>
		public static Rect GetPositionOnScreen(Vector2 size)
		{
			var result = new Rect(GUIUtility.GUIToScreenPoint(Event.current.mousePosition) - (size * 0.5f), size);

			return result;
			//if (IsOnScreen(result)) return result;
			//return new Rect((new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)) - (size * 0.5f), size);
		}

		/// <summary>
		/// Deselects any buttons, moves the cursor out of text boxes, etc.
		/// </summary>
		public static void ResetControls()
		{
			GUIUtility.keyboardControl = 0;
		}
	}
}