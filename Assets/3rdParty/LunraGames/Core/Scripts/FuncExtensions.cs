using System;

namespace LunraGames
{
	public static class FuncExtensions
	{
		public static Func<T> GetEmpty<T>() { return () => { return default(T); }; }
		public static Func<T> GetEmpty<T>(T value) { return () => { return value; }; }

		/// <summary>
		/// For some reason my version of unity doesn't have this... so here it is...
		/// </summary>
		public delegate TResult Func<T1, T2, T3, T4, T5, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
	}
}