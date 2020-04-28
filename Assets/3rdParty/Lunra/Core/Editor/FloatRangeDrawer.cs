using Lunra.Core;
using UnityEditor;
using UnityEngine;

namespace Lunra.Editor.Core
{
	[CustomPropertyDrawer(typeof(FloatRange))]
	public class FloatRangeDrawer : PropertyDrawer
	{
		const float RowHeight = 16f;
		const float RowSpacing = 2f;

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return (RowHeight * 1f) + RowSpacing;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			// Using BeginProperty / EndProperty on the parent property means that
			// prefab override logic works on the entire property.
			EditorGUI.BeginProperty(position, label, property);

			var indent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;

			// Calculate rects
			var rect = new Rect(position.x, position.y, position.width, RowHeight);

			var primaryProperty = property.FindPropertyRelative("primary");
			var secondaryProperty = property.FindPropertyRelative("secondary");

			var result = EditorGUI.Vector2Field(rect, label, new Vector2(primaryProperty.floatValue, secondaryProperty.floatValue));
			primaryProperty.floatValue = result.x;
			secondaryProperty.floatValue = result.y;

			// Set indent back to what it was
			EditorGUI.indentLevel = indent;

			EditorGUI.EndProperty();
		}
	}
}