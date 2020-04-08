using System;
using System.Linq;
using System.Collections.Generic;

namespace LunraGames
{
	public static class EnumExtensions
	{
		public static T[] GetValues<T>()
		{
			return Enum.GetValues(typeof(T)).Cast<T>().ToArray();
		}

		public static T[] GetValues<T>(params T[] except)
		{
			return Enum.GetValues(typeof(T)).Cast<T>().Except(except).ToArray();
		}
	}
}
