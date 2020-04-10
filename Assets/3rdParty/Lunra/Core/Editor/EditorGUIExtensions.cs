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
		
		public static void PushIndent()
		{
			EditorGUI.indentLevel++;
		}

		public static void PopIndent()
		{
			EditorGUI.indentLevel--;
		}
	}
}