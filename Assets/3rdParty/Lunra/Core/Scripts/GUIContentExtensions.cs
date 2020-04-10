using UnityEngine;

namespace Lunra.Core
{
	// ReSharper disable once InconsistentNaming
	public static class GUIContentExtensions
	{
		// I tried making this an extension method, but it won't work. Something
		// to do with how parameterless static extensions work, dunno.
		public static bool IsNullOrNone(GUIContent content)
		{
			return content == null || (string.IsNullOrEmpty(content.text) && string.IsNullOrEmpty(content.tooltip) && content.image == null);
		}
	}
}