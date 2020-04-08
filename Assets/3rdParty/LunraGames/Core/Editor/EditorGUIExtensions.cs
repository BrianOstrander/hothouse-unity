using System.Linq;
using System.Collections.Generic;

using UnityEngine;

using UnityEditor;

namespace LunraGamesEditor
{
	public static class EditorGUIExtensions
	{
		enum ChangeCheckStates
		{
			Listening,
			Paused
		}

		static int CurrentLevel;
		static Stack<ChangeCheckStates> ChangeCheckStateStack = new Stack<ChangeCheckStates>();
		static Stack<bool> ChangeValueStack = new Stack<bool>();

		static bool IsActive { get { return 0 < CurrentLevel; } }

		public static void BeginChangeCheck()
		{
			if (IsActive)
			{
				if (ChangeCheckStateStack.Peek() == ChangeCheckStates.Paused)
				{
					Debug.LogError("Cannot begin a change check in the middle of a pause.");
					return;
				}

				// If anything messes up, it's probably because of the next two lines...
				var previous = false;
				try { previous = EditorGUI.EndChangeCheck(); } catch {}

				previous = ChangeValueStack.Pop() || previous;
				ChangeValueStack.Push(previous);
			}

			ChangeValueStack.Push(false);

			ChangeCheckStateStack.Push(ChangeCheckStates.Listening);
			CurrentLevel++;

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
			if (ChangeCheckStateStack.Peek() == ChangeCheckStates.Paused)
			{
				Debug.LogError("Cannot end a change check that has been paused.");
				return false;
			}

			ChangeCheckStateStack.Pop();
			CurrentLevel--;
			var result = ChangeValueStack.Pop();
			result = EditorGUI.EndChangeCheck() || result;
			return result;
		}

		public static void PauseChangeCheck()
		{
			if (!IsActive) return;
			if (ChangeCheckStateStack.Peek() == ChangeCheckStates.Paused) return;

			ChangeCheckStateStack.Push(ChangeCheckStates.Paused);

			var current = EditorGUI.EndChangeCheck();
			current = ChangeValueStack.Pop() || current;
			ChangeValueStack.Push(current);
		}

		public static void UnPauseChangeCheck()
		{
			if (!IsActive) return;
			if (ChangeCheckStateStack.Peek() != ChangeCheckStates.Paused)
			{
				Debug.LogError("Cannot unpause a change check that has not been paused.");
				return;
			}

			ChangeCheckStateStack.Pop();

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