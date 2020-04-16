using UnityEngine;

namespace Lunra.Editor.Core
{
    // ReSharper disable once InconsistentNaming
    public static class GUILayoutExtensions
    {
        public static void BeginVertical(GUIStyle style, Color? color, bool useColor = true, params GUILayoutOption[] options)
        {
            BeginVertical(style, color ?? GUI.color, useColor, options);
        }

        public static void BeginVertical(GUIStyle style, Color color, bool useColor = true, params GUILayoutOption[] options)
        {
            if (useColor) GUIExtensions.PushColor(color);
            GUILayout.BeginVertical(style, options);
            if (useColor) GUIExtensions.PopColor();
        }

        public static void BeginVertical(GUIStyle style, Color primaryColor, Color secondaryColor, bool isPrimary, params GUILayoutOption[] options)
        {
            GUIExtensions.PushColor(isPrimary ? primaryColor : secondaryColor);
            GUILayout.BeginVertical(style, options);
            GUIExtensions.PopColor();
        }

        public static void EndVertical() => GUILayout.EndVertical(); 

        public static void BeginHorizontal(GUIStyle style, Color color, bool useColor = true, params GUILayoutOption[] options)
        {
            if (useColor) GUIExtensions.PushColor(color);
            GUILayout.BeginHorizontal(style, options);
            if (useColor) GUIExtensions.PopColor();
        }

        public static void EndHorizontal() => GUILayout.EndHorizontal();
    }
}