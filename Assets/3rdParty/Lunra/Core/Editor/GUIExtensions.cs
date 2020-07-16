using System.Collections.Generic;
using Lunra.Core;
using UnityEngine;

namespace Lunra.Editor.Core
{
    // ReSharper disable once InconsistentNaming
    public static class GUIExtensions
    {
	    struct ColorCombined
	    {
		    public Color ContentColor;
		    public Color BackgroundColor;
		    public Color Color;
	    }
	    
	    static Stack<Color> colorStack = new Stack<Color>();
	    static Stack<ColorCombined> colorCombinedStack = new Stack<ColorCombined>();
	    static Stack<Color> backgroundColorStack = new Stack<Color>();
	    static Stack<Color> contentColorStack = new Stack<Color>();
	    static Stack<bool> enabledStack = new Stack<bool>();
	    
        public static void PushColor(Color color)
		{
			colorStack.Push(GUI.color);
			GUI.color = color;
		}

		public static void PopColor()
		{
			if (colorStack.Count == 0) return;
			GUI.color = colorStack.Pop();
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
		/// <param name="useColor">Is the color enabled.</param>
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

			colorCombinedStack.Push(current);

			if (contentColor.HasValue) GUI.contentColor = contentColor.Value;
			if (backgroundColor.HasValue) GUI.backgroundColor = backgroundColor.Value;
			if (color.HasValue) GUI.color = color.Value;
		}

		public static void PopColorCombined()
		{
			if (colorCombinedStack.Count == 0) return;
			var target = colorCombinedStack.Pop();
			GUI.contentColor = target.ContentColor;
			GUI.backgroundColor = target.BackgroundColor;
			GUI.color = target.Color;
		}

		public static void PushBackgroundColor(Color backgroundColor)
		{
			backgroundColorStack.Push(GUI.backgroundColor);
			GUI.backgroundColor = backgroundColor;
		}

		public static void PopBackgroundColor()
		{
			if (backgroundColorStack.Count == 0) return;
			GUI.backgroundColor = backgroundColorStack.Pop();
		}

		public static void PushContentColor(Color color)
		{
			contentColorStack.Push(GUI.contentColor);
			GUI.contentColor = color;
		}

		public static void PopContentColor()
		{
			if (contentColorStack.Count == 0) return;
			GUI.contentColor = contentColorStack.Pop();
		}

		public static void PushEnabled(
			bool enabled,
			bool ignoreExistingState = false
		)
		{
			enabledStack.Push(GUI.enabled);
			GUI.enabled = ignoreExistingState ? enabled : GUI.enabled && enabled;
		}

		public static void PopEnabled()
		{
			if (enabledStack.Count == 0) return;
			GUI.enabled = enabledStack.Pop();
		}
    }
}