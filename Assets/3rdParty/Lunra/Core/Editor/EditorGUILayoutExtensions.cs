using System;
using System.Linq;

using UnityEngine;

using UnityEditor;

using Lunra.Core;

namespace Lunra.Editor.Core
{
	// ReSharper disable once InconsistentNaming
	public static class EditorGUILayoutExtensions
	{
		static class Constants
		{
			public const float LabelWidth = 145f;
			
			static GUIStyle richTextStyle;

			public static GUIStyle RichTextStyle
			{
				get
				{
					if (richTextStyle == null)
					{
						richTextStyle = new GUIStyle(EditorStyles.label);
						richTextStyle.richText = true;
					}

					return richTextStyle;
				}
			}
		}

		public static T HelpfulEnumPopupValidation<T>(
			GUIContent content,
			string primaryReplacement,
			T value,
			Func<T, Color?> getDefaultValueColor,
			T[] options = null,
			params GUILayoutOption[] guiOptions
		) where T : struct, Enum
		{
			var defaultValueColor = getDefaultValueColor(value);
			GUIExtensions.PushColorValidation(defaultValueColor);
			var result = HelpfulEnumPopup(
				content,
				primaryReplacement,
				value,
				options,
				guiOptions
			);
			GUIExtensions.PopColorValidation(defaultValueColor);
			return result;
		}

		public static T HelpfulEnumPopupValueValidation<T>(
			string primaryReplacement,
			T value,
			Func<T, Color?> getDefaultValueColor,
			params GUILayoutOption[] guiOptions
		) where T : struct, Enum
		{
			var defaultValueColor = getDefaultValueColor(value);
			GUIExtensions.PushColorValidation(defaultValueColor);
			var result = HelpfulEnumPopupValue(
				primaryReplacement,
				value,
				guiOptions
			);
			GUIExtensions.PopColorValidation(defaultValueColor);
			return result;
		}

		public static T HelpfulEnumPopupValidation<T>(
			GUIContent content,
			string primaryReplacement,
			T value,
			Color? defaultValueColor,
			T[] options = null,
			params GUILayoutOption[] guiOptions
		) where T : struct, Enum
		{
			if (!EnumsEqual(value, default)) defaultValueColor = null;
			GUIExtensions.PushColorValidation(defaultValueColor);
			var result = HelpfulEnumPopup(
				content,
				primaryReplacement,
				value,
				options,
				guiOptions
			);
			GUIExtensions.PopColorValidation(defaultValueColor);
			return result;
		}

		public static T HelpfulEnumPopupValueValidation<T>(
			string primaryReplacement,
			T value,
			Color? defaultValueColor,
			params GUILayoutOption[] guiOptions
		) where T : struct, Enum
		{
			if (!EnumsEqual(value, default)) defaultValueColor = null;
			GUIExtensions.PushColorValidation(defaultValueColor);
			var result = HelpfulEnumPopupValue(
				primaryReplacement,
				value,
				guiOptions
			);
			GUIExtensions.PopColorValidation(defaultValueColor);
			return result;
		}

		public static T HelpfulEnumPopupValueValidation<T>(
			string primaryReplacement,
			T value,
			Color? defaultValueColor,
			T[] options,
			params GUILayoutOption[] guiOptions
		) where T : struct, Enum
		{
			if (!EnumsEqual(value, default)) defaultValueColor = null;
			GUIExtensions.PushColorValidation(defaultValueColor);
			var result = HelpfulEnumPopupValue(
				primaryReplacement,
				value,
				options,
				null,
				guiOptions
			);
			GUIExtensions.PopColorValidation(defaultValueColor);
			return result;
		}

		public static T HelpfulEnumPopup<T>(
			GUIContent content,
			string primaryReplacement,
			T value,
			T[] options = null,
			params GUILayoutOption[] guiOptions
		) where T : struct, Enum
		{
			T result;
			GUILayout.BeginHorizontal();
			{
				EditorGUILayout.PrefixLabel(content);
				var wasIndent = EditorGUI.indentLevel;
				EditorGUI.indentLevel = 0;
				result = HelpfulEnumPopupValue(
					primaryReplacement,
					value,
					options,
					null,
					guiOptions
				);
				EditorGUI.indentLevel = wasIndent;
			}
			GUILayout.EndHorizontal();
			return result;
		}

		public static T HelpfulEnumPopupValue<T>(
			string primaryReplacement,
			T value,
			params GUILayoutOption[] guiOptions
		) where T : struct, Enum
		{
			return HelpfulEnumPopupValue(
				primaryReplacement,
				value,
				null,
				guiOptions
			);
		}

		public static T HelpfulEnumPopupValue<T>(
			string primaryReplacement,
			T value,
			T[] options,
			params GUILayoutOption[] guiOptions
		) where T : struct, Enum
		{
			return HelpfulEnumPopupValue(
				primaryReplacement,
				value,
				options,
				null,
				guiOptions
			);
		}

		/// <summary>
		/// Renames the first enum entry, useful for adding a "- Select X -" option.
		/// </summary>
		/// <returns>The enum popup.</returns>
		/// <param name="primaryReplacement">Primary replacement.</param>
		/// <param name="value">Value.</param>
		/// <param name="options">Enum options</param>
		/// <param name="style">GUIStyle</param>
		/// <param name="guiOptions">GUILayout options</param>
		public static T HelpfulEnumPopupValue<T>(
			string primaryReplacement, 
			T value,
			T[] options,
			GUIStyle style,
			params GUILayoutOption[] guiOptions
		) where T : struct, Enum
		{
			var name = Enum.GetName(value.GetType(), value);
			var originalNames = options == null ? Enum.GetNames(value.GetType()) : options.Select(o => Enum.GetName(value.GetType(), o)).ToArray();
			var names = originalNames.ToArray();
			if (!string.IsNullOrEmpty(primaryReplacement)) names[0] = primaryReplacement;
			var selection = 0;
			foreach (var currName in names)
			{
				if (currName == name) break;
				selection++;
			}
			selection = selection == names.Length ? 0 : selection;
			if (style == null) selection = EditorGUILayout.Popup(selection, names, guiOptions);
			else selection = EditorGUILayout.Popup(selection, names, style, guiOptions);

			return (T)Enum.Parse(value.GetType(), originalNames[selection]);
		}

		public static int IntegerEnumPopup(
			GUIContent content,
			int value,
			Type enumerationType
		)
		{
			int result;
			GUILayout.BeginHorizontal();
			{
				EditorGUILayout.PrefixLabel(content);
				var wasIndent = EditorGUI.indentLevel;
				EditorGUI.indentLevel = 0;
				result = IntegerEnumPopupValue(
					value,
					enumerationType
				);
				EditorGUI.indentLevel = wasIndent;
			}
			GUILayout.EndHorizontal();
			return result;
		}

		public static int IntegerEnumPopupValue(
			int value,
			Type enumerationType
		)
		{
			var enumerationValues = Enum.GetValues(enumerationType);

			var enumerationNames = new string[enumerationValues.Length];
			var enumerationIndices = new int[enumerationNames.Length];

			for (var i = 0; i < enumerationValues.Length; i++)
			{
				var currentEnumerationValue = enumerationValues.GetValue(i);
				enumerationNames[i] = Enum.GetName(enumerationType, currentEnumerationValue);
				enumerationIndices[i] = (int)currentEnumerationValue;
			}

			return EditorGUILayout.IntPopup(
				value,
				enumerationNames,
				enumerationIndices
			);
		}

		public static bool XButton(bool small = false)
		{
			GUIExtensions.PushColorValidation(Color.red);
			bool clicked;
			if (small) clicked = GUILayout.Button("x", EditorStyles.miniButton, GUILayout.Width(20f));
			else clicked = GUILayout.Button("x", GUILayout.Width(20f));
			GUIExtensions.PopColorValidation();
			return clicked;
		}

		public static string[] StringArray(
			GUIContent content,
			string[] values,
			string defaultValue = null,
			Color? color = null
		)
		{
			GUILayoutExtensions.BeginVertical(EditorStyles.helpBox, color, color.HasValue);
			{
				values = StringArrayValue(
					content,
					values,
					defaultValue
				);
			}
			GUILayoutExtensions.EndVertical();
			return values;
		}

		public static string[] StringArrayValue(
			GUIContent content,
			string[] values,
			string defaultValue = null
		)
		{
			if (values == null) throw new ArgumentNullException(nameof(values));

			GUILayout.BeginHorizontal();
			{
				GUILayout.Label(content);
				if (GUILayout.Button("Prepend", EditorStyles.miniButtonLeft, GUILayout.Width(90f))) values = values.Prepend(defaultValue).ToArray();
				if (GUILayout.Button("Append", EditorStyles.miniButtonRight, GUILayout.Width(90f))) values = values.Append(defaultValue).ToArray();
			}
			GUILayout.EndHorizontal();

			if (values.Length == 0) return values;

			int? deletedIndex = null;
			GUILayout.BeginVertical();
			{
				GUILayout.Space(4f);
				for (var i = 0; i < values.Length; i++)
				{
					GUILayout.BeginHorizontal();
					{
						GUILayout.Space(16f);
						GUILayout.Label("[ "+i+" ]", GUILayout.Width(32f));
						values[i] = EditorGUILayout.TextField(values[i]);
						if (XButton()) deletedIndex = i;
					}
					GUILayout.EndHorizontal();
				}
			}
			GUILayout.EndVertical();
			if (deletedIndex.HasValue)
			{
				var list = values.ToList();
				list.RemoveAt(deletedIndex.Value);
				values = list.ToArray();
			}
			return values;
		}

		public static T[] EnumButtonArrayValue<T>(
			T[] values,
			T[] options = null
		)
			where T : struct, Enum
		{
			return EnumButtonArray(
				GUIContent.none, 
				values,
				options
			);
		}
		
		public static T[] EnumButtonArray<T>(
			GUIContent content,
			T[] values,
			T[] options = null
		)
			where T : struct, Enum
		{
			GUILayout.BeginHorizontal();
			{
				if (!GUIContentExtensions.IsNullOrNone(content))
				{
					EditorGUILayout.LabelField(
						content,
						GUILayout.Width(Constants.LabelWidth)
					);
				}

				options = options ?? EnumExtensions.GetValues(default(T));

				for (var i = 0; i < options.Length; i++)
				{
					var buttonStyle = EditorStyles.miniButtonMid;
					if (1 < options.Length)
					{
						if (i == 0) buttonStyle = EditorStyles.miniButtonLeft;
						else if (i == (options.Length - 1)) buttonStyle = EditorStyles.miniButtonRight;
					}
					else buttonStyle = EditorStyles.miniButton;

					var oldValue = values.Contains(options[i]);
					var newValue = GUILayout.Toggle(
						oldValue,
						ObjectNames.NicifyVariableName(options[i].ToString()),
						buttonStyle
					);

					if (oldValue != newValue)
					{
						if (newValue) values = values.Append(options[i]).ToArray();
						else values = values.ExceptOne(options[i]).ToArray();
					}
				}
			}
			GUILayout.EndHorizontal();
			return values;
		}

		public static string TextAreaWrapped(string value, params GUILayoutOption[] options)
		{
			return TextAreaWrapped(GUIContent.none, value, options);
		}

		public static string TextAreaWrapped(GUIContent content, string value, params GUILayoutOption[] options)
		{
			var textStyle = EditorStylesExtensions.PushTextAreaWordWrap(true);
			{
				if (!GUIContentExtensions.IsNullOrNone(content)) GUILayout.Label(content);
				value = EditorGUILayout.TextArea(value, textStyle, options);
			}
			EditorStylesExtensions.PopTextAreaWordWrap();

			return value;
		}

		public static bool ToggleButtonCompact(string label, bool value, GUIStyle style = null, params GUILayoutOption[] options)
		{
			return ToggleButtonValue(value, label, label, style, options);
		}

		public static bool ToggleButtonValue(bool value, string trueText = "True", string falseText = "False", GUIStyle style = null, params GUILayoutOption[] options)
		{
			options = options.Prepend(GUILayout.Width(48f)).ToArray();
			var wasValue = value;

			GUIExtensions.PushColorValidation(Color.red, !wasValue);
			{
				var text = value ? trueText : falseText;

				if (style == null || style == GUIStyle.none)
				{
					if (GUILayout.Button(text, options)) value = !value;
				}
				else
				{
					if (GUILayout.Button(text, style, options)) value = !value;
				}
			}
			GUIExtensions.PopColorValidation(!wasValue);

			return value;
		}

		public static bool ToggleButton(GUIContent content, bool value, string trueText = "True", string falseText = "False")
		{
			GUILayout.BeginHorizontal();
			{
				EditorGUILayout.PrefixLabel(content);
				var buttonStyle = EditorStylesExtensions.PushButtonTextAnchor(TextAnchor.MiddleLeft);
				{
					if (GUILayout.Button(value ? trueText : falseText, buttonStyle)) value = !value;
				}
				EditorStylesExtensions.PopButtonTextAnchor();
			}
			GUILayout.EndHorizontal();
			return value;
		}

		public static bool ToggleButtonArray(bool value, string trueText = "True", string falseText = "False")
		{
			return ToggleButtonArray(GUIContent.none, value, trueText, falseText);
		}

		public static bool ToggleButtonArray(GUIContent label, bool value, string trueText = "True", string falseText = "False")
		{
			var wasValue = value;

			GUILayout.BeginHorizontal();
			{
				if (label != null && label != GUIContent.none) GUILayout.Label(label, GUILayout.Width(Constants.LabelWidth));

				var notSelectedContentColor = Color.gray.NewV(0.75f);
				var notSelectedBackgroundColor = Color.gray.NewV(0.65f);

				if (!wasValue) GUIExtensions.PushColorCombined(notSelectedContentColor, notSelectedBackgroundColor);
				if (GUILayout.Button(trueText, EditorStyles.miniButtonLeft, GUILayout.ExpandWidth(false))) value = true;
				if (!wasValue) GUIExtensions.PopColorCombined();

				if (wasValue) GUIExtensions.PushColorCombined(notSelectedContentColor, notSelectedBackgroundColor);
				if (GUILayout.Button(falseText, EditorStyles.miniButtonRight, GUILayout.ExpandWidth(false))) value = false;
				if (wasValue) GUIExtensions.PopColorCombined();
			}
			GUILayout.EndHorizontal();
			return value;
		}
		
		public static Vector2 Vector2FieldCompact(
			GUIContent label,
			Vector2 value 
		)
		{
			var x = value.x;
			var y = value.y;
			
			GUILayout.BeginHorizontal();
			{
				if (label != null && label != GUIContent.none) GUILayout.Label(label, GUILayout.Width(Constants.LabelWidth));
				x = EditorGUILayout.FloatField(x);
				y = EditorGUILayout.FloatField(y);
			}
			GUILayout.EndHorizontal();

			return new Vector2(x, y);
		}
		
		public static void RichTextLabel(string text) => GUILayout.Label(text, Constants.RichTextStyle);

		#region Shared
		static bool EnumsEqual<T>(
			T value0,
			T value1
		) where T : struct, Enum
		{
			// TODO: Should this use integer values instead?
			return Enum.GetName(value0.GetType(), value0) == Enum.GetName(value1.GetType(), value1);
		}
		#endregion
	}
}