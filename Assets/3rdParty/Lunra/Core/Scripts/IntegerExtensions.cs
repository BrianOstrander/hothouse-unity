using UnityEngine;

namespace Lunra.Core
{
	public static class IntegerExtensions
	{
		public static Color ColorFromIndex(this int value)
		{
			return Color.cyan.NewH((value * 0.618034f) % 1f);
		}
	}
}