﻿namespace Lunra.Core
{
	public static class StringExtensions 
	{
		public static bool IsNullOrWhiteSpace(string value)
		{
			return string.IsNullOrEmpty(value) || value.Trim().Length == 0;
		}

		public static string TruncateStart(string value, int length, string truncation = "...")
		{
			if (value.Length <= length) return value;
			return truncation + value.Substring(value.Length - length);
		}

		public static string GetNonNullOrEmpty(string value, string defaultValue) => string.IsNullOrEmpty(value) ? defaultValue : value;

		public static string Wrap(string value, string begin, string end) => begin + value + end;
	}
}