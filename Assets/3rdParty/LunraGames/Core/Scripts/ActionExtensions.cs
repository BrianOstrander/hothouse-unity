using System;

namespace LunraGames
{
	public static class ActionExtensions
	{
		public static Action Empty { get { return () => {}; } }

		public static Action<T> GetEmpty<T>() { return (param0) => {}; }
		public static Action<T0, T1> GetEmpty<T0, T1>() { return (param0, param1) => {}; }
		public static Action<T0, T1, T2> GetEmpty<T0, T1, T2>() { return (param0, param1, param2) => {}; }
		public static Action<T0, T1, T2, T3> GetEmpty<T0, T1, T2, T3>() { return (param0, param1, param2, param3) => {}; }
	}
}