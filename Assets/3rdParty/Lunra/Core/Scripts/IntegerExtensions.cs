using UnityEngine;

namespace Lunra.Core
{
	public static class IntegerExtensions
	{
		public static Color ColorFromIndex(this int value)
		{
			return Color.cyan.NewH((value * 0.618034f) % 1f);
		}
		
		public static string Pad(
			this int value,
			int minimumPadding = 8,
			char padding = '0'
		)
		{
			var result = value.ToString();

			return result.Length < minimumPadding ? result.PadLeft(minimumPadding, padding) : result;
		}
	}
}