using UnityEngine;

namespace LunraGamesEditor
{
    // ReSharper disable once InconsistentNaming
    public class GUIUtilityExtensions
    {
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