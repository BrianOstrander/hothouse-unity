using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;

using UnityEditor;

using LunraGames;
using LunraGames.SubLight;

namespace LunraGamesEditor
{
	public static class EditorGUILayoutExtensions
	{
		public static class Constants
		{
			public const float LabelWidth = 145f;
		}
		
		struct ColorCombined
		{
			public Color ContentColor;
			public Color BackgroundColor;
			public Color Color;
		}

		static Stack<Color> ColorStack = new Stack<Color>();
		static Stack<ColorCombined> ColorCombinedStack = new Stack<ColorCombined>();
		static Stack<Color> BackgroundColorStack = new Stack<Color>();
		static Stack<Color> ContentColorStack = new Stack<Color>();
		static Stack<bool> EnabledStack = new Stack<bool>();
		static Stack<bool> TextAreaWordWrapStack = new Stack<bool>();
		static Stack<TextAnchor> ButtonTextAnchorStack = new Stack<TextAnchor>();

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
			PushColorValidation(defaultValueColor);
			var result = HelpfulEnumPopup(
				content,
				primaryReplacement,
				value,
				options,
				guiOptions
			);
			PopColorValidation(defaultValueColor);
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
			PushColorValidation(defaultValueColor);
			var result = HelpfulEnumPopupValue(
				primaryReplacement,
				value,
				guiOptions
			);
			PopColorValidation(defaultValueColor);
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
			PushColorValidation(defaultValueColor);
			var result = HelpfulEnumPopup(
				content,
				primaryReplacement,
				value,
				options,
				guiOptions
			);
			PopColorValidation(defaultValueColor);
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
			PushColorValidation(defaultValueColor);
			var result = HelpfulEnumPopupValue(
				primaryReplacement,
				value,
				guiOptions
			);
			PopColorValidation(defaultValueColor);
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
			PushColorValidation(defaultValueColor);
			var result = HelpfulEnumPopupValue(
				primaryReplacement,
				value,
				options,
				null,
				guiOptions
			);
			PopColorValidation(defaultValueColor);
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
			return HelpfulEnumPopupValue<T>(
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

		public static void PushColor(Color color)
		{
			ColorStack.Push(GUI.color);
			GUI.color = color;
		}

		public static void PopColor()
		{
			if (ColorStack.Count == 0) return;
			GUI.color = ColorStack.Pop();
		}

		/// <summary>
		/// Works like PushColorCombined, but handles the tinting automatically.
		/// </summary>
		/// <param name="color">Color.</param>
		public static void PushColorValidation(Color? color)
		{
			if (color.HasValue) PushColorValidation(color.Value);
		}

		public static void PopColorValidation(Color? color)
		{
			if (color.HasValue) PopColorValidation();
		}

		/// <summary>
		/// Works like PushColorCombined, but handles the tinting automatically.
		/// </summary>
		/// <param name="color">Color.</param>
		public static void PushColorValidation(Color color, bool useColor = true)
		{
			if (useColor) PushColorCombined(color.NewS(0.25f), color.NewS(0.65f));
		}

		public static void PopColorValidation(bool useColor = true)
		{	
			if (useColor) PopColorCombined();
		}

		public static void PushColorCombined(
			Color? contentColor = null,
			Color? backgroundColor = null,
			Color? color = null
		)
		{
			var current = new ColorCombined
			{
				ContentColor = GUI.contentColor,
				BackgroundColor = GUI.backgroundColor,
            	Color = GUI.color,
			};

			ColorCombinedStack.Push(current);

			if (contentColor.HasValue) GUI.contentColor = contentColor.Value;
			if (backgroundColor.HasValue) GUI.backgroundColor = backgroundColor.Value;
			if (color.HasValue) GUI.color = color.Value;
		}

		public static void PopColorCombined()
		{
			if (ColorCombinedStack.Count == 0) return;
			var target = ColorCombinedStack.Pop();
			GUI.contentColor = target.ContentColor;
			GUI.backgroundColor = target.BackgroundColor;
			GUI.color = target.Color;
		}

		public static void PushBackgroundColor(Color backgroundColor)
		{
			BackgroundColorStack.Push(GUI.backgroundColor);
			GUI.backgroundColor = backgroundColor;
		}

		public static void PopBackgroundColor()
		{
			if (BackgroundColorStack.Count == 0) return;
			GUI.backgroundColor = BackgroundColorStack.Pop();
		}

		public static void PushContentColor(Color color)
		{
			ContentColorStack.Push(GUI.contentColor);
			GUI.contentColor = color;
		}

		public static void PopContentColor()
		{
			if (ContentColorStack.Count == 0) return;
			GUI.contentColor = ContentColorStack.Pop();
		}

		public static void PushEnabled(bool enabled)
		{
			EnabledStack.Push(GUI.enabled);
			GUI.enabled &= enabled;
		}

		public static void PopEnabled()
		{
			if (EnabledStack.Count == 0) return;
			GUI.enabled = EnabledStack.Pop();
		}

		public static GUIStyle PushTextAreaWordWrap(bool enabled)
		{
			TextAreaWordWrapStack.Push(EditorStyles.textArea.wordWrap);
			EditorStyles.textArea.wordWrap = enabled;
			return EditorStyles.textArea;
		}

		public static void PopTextAreaWordWrap()
		{
			if (TextAreaWordWrapStack.Count == 0) return;
			var popped = TextAreaWordWrapStack.Pop();
			EditorStyles.textArea.wordWrap = popped;
		}

		public static GUIStyle PushButtonTextAnchor(TextAnchor anchor)
		{
			ButtonTextAnchorStack.Push(GUI.skin.button.alignment);
			GUI.skin.button.alignment = anchor;
			return GUI.skin.button;
		}

		public static void PopButtonTextAnchor()
		{
			if (ButtonTextAnchorStack.Count == 0) return;
			var popped = ButtonTextAnchorStack.Pop();
			GUI.skin.button.alignment = popped;
		}

		public static void PushIndent()
		{
			EditorGUI.indentLevel++;
		}

		public static void PopIndent()
		{
			EditorGUI.indentLevel--;
		}

		public static bool XButton(bool small = false)
		{
			PushColorValidation(Color.red);
			var clicked = false;
			if (small) clicked = GUILayout.Button("x", EditorStyles.miniButton, GUILayout.Width(20f));
			else clicked = GUILayout.Button("x", GUILayout.Width(20f));
			PopColorValidation();
			return clicked;
		}

		public static string[] StringArray(
			GUIContent content,
			string[] values,
			string defaultValue = null,
			Color? color = null
		)
		{
			BeginVertical(EditorStyles.helpBox, color, color.HasValue);
			{
				values = StringArrayValue(
					content,
					values,
					defaultValue
				);
			}
			EndVertical();
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
		
		public static string TextDynamic(GUIContent content, string value, int lengthLimit = 32, bool leftOffset = true)
		{
			var nullContent = GUIContentExtensions.IsNullOrNone(content);
			lengthLimit = nullContent ? lengthLimit * 2 : lengthLimit;
			if (string.IsNullOrEmpty(value) || value.Length < lengthLimit)
			{
				// Is Field
				GUILayout.BeginHorizontal();
				{
					// Insert zero width button so we preserve the focused UI element when switching to an area.
					GUILayout.Button(GUIContent.none, GUIStyle.none, GUILayout.Width(0f));
					if (leftOffset) GUILayout.Space(-4f);
					if (nullContent) value = EditorGUILayout.TextField(value);
					else value = EditorGUILayout.TextField(content, value);
					PushEnabled(value != null);
					if (GUILayout.Button("Set Null", GUILayout.Width(54f), GUILayout.Height(14f))) value = null;
					PopEnabled();
				}
				GUILayout.EndHorizontal();
				GUILayout.Space(3f);
			}
			else
			{
				// Is Area
				var textStyle = PushTextAreaWordWrap(true);
				{
					GUILayout.BeginHorizontal();
					{
						if (!nullContent) GUILayout.Label(content);
						PushEnabled(value != null);
						if (GUILayout.Button("Set Null", GUILayout.Width(54f), GUILayout.Height(14f))) value = null;
						PopEnabled();
					}
					GUILayout.EndHorizontal();
					value = EditorGUILayout.TextArea(value, textStyle);
				}
				PopTextAreaWordWrap();
			}
			return value;
		}

		public static string TextAreaWrapped(string value, params GUILayoutOption[] options)
		{
			return TextAreaWrapped(GUIContent.none, value, options);
		}

		public static string TextAreaWrapped(GUIContent content, string value, params GUILayoutOption[] options)
		{
			var textStyle = PushTextAreaWordWrap(true);
			{
				if (!GUIContentExtensions.IsNullOrNone(content)) GUILayout.Label(content);
				value = EditorGUILayout.TextArea(value, textStyle, options);
			}
			PopTextAreaWordWrap();

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

			PushColorValidation(Color.red, !wasValue);
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
			PopColorValidation(!wasValue);

			return value;
		}

		public static bool ToggleButton(GUIContent content, bool value, string trueText = "True", string falseText = "False")
		{
			GUILayout.BeginHorizontal();
			{
				EditorGUILayout.PrefixLabel(content);
				var buttonStyle = PushButtonTextAnchor(TextAnchor.MiddleLeft);
				{
					if (GUILayout.Button(value ? trueText : falseText, buttonStyle)) value = !value;
				}
				PopButtonTextAnchor();
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

				if (!wasValue) PushColorCombined(notSelectedContentColor, notSelectedBackgroundColor);
				if (GUILayout.Button(trueText, EditorStyles.miniButtonLeft, GUILayout.ExpandWidth(false))) value = true;
				if (!wasValue) PopColorCombined();

				if (wasValue) PushColorCombined(notSelectedContentColor, notSelectedBackgroundColor);
				if (GUILayout.Button(falseText, EditorStyles.miniButtonRight, GUILayout.ExpandWidth(false))) value = false;
				if (wasValue) PopColorCombined();
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
		
		public static void BeginVertical(GUIStyle style, Color? color, bool useColor = true, params GUILayoutOption[] options)
		{
			BeginVertical(style, color.HasValue ? color.Value : GUI.color, useColor, options);
		}

		public static void BeginVertical(GUIStyle style, Color color, bool useColor = true, params GUILayoutOption[] options)
		{
			if (useColor) PushColor(color);
			GUILayout.BeginVertical(style, options);
			if (useColor) PopColor();
		}

		public static void BeginVertical(GUIStyle style, Color primaryColor, Color secondaryColor, bool isPrimary, params GUILayoutOption[] options)
		{
			PushColor(isPrimary ? primaryColor : secondaryColor);
			GUILayout.BeginVertical(style, options);
			PopColor();
		}

		public static void EndVertical()
		{
			GUILayout.EndVertical();
		}

		public static void BeginHorizontal(GUIStyle style, Color color, bool useColor = true, params GUILayoutOption[] options)
		{
			if (useColor) PushColor(color);
			GUILayout.BeginHorizontal(style, options);
			if (useColor) PopColor();
		}

		public static void EndHorizontal()
		{
			GUILayout.EndHorizontal();
		}
		
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