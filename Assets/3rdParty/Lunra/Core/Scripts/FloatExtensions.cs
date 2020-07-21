using UnityEngine;

namespace Lunra.Core
{
	public static class FloatExtensions
	{
		public static string ToBarString(
			this float value,
			int characterCount = 10,
			string filled = "=",
			string unfilled = "-",
			float minimum = 0f,
			float maximum = 1f
		)
		{
			var normalized = Mathf.Clamp(value, minimum, maximum) / (maximum - minimum);
			var result = string.Empty;
			for (var i = 0f; i < characterCount; i++)
			{
				result += (i / (characterCount - 1)) <= normalized ? filled : unfilled;
			}

			return result;
		}
	}
}