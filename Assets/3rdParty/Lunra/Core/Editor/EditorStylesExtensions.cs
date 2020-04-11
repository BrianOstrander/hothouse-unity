using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Lunra.Editor.Core
{
    public static class EditorStylesExtensions
    {
        static Stack<bool> textAreaWordWrapStack = new Stack<bool>();
        static Stack<TextAnchor> buttonTextAnchorStack = new Stack<TextAnchor>();
        
        public static GUIStyle PushTextAreaWordWrap(bool enabled)
        {
            textAreaWordWrapStack.Push(EditorStyles.textArea.wordWrap);
            EditorStyles.textArea.wordWrap = enabled;
            return EditorStyles.textArea;
        }

        public static void PopTextAreaWordWrap()
        {
            if (textAreaWordWrapStack.Count == 0) return;
            var popped = textAreaWordWrapStack.Pop();
            EditorStyles.textArea.wordWrap = popped;
        }

        public static GUIStyle PushButtonTextAnchor(TextAnchor anchor)
        {
            buttonTextAnchorStack.Push(GUI.skin.button.alignment);
            GUI.skin.button.alignment = anchor;
            return GUI.skin.button;
        }

        public static void PopButtonTextAnchor()
        {
            if (buttonTextAnchorStack.Count == 0) return;
            var popped = buttonTextAnchorStack.Pop();
            GUI.skin.button.alignment = popped;
        }
    }
}