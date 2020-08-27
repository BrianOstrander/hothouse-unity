using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Lunra.Core
{
	public static class StringExtensions 
	{
		// TODO: Convert to use "this string"
		public static bool IsNullOrWhiteSpace(string value)
		{
			return string.IsNullOrEmpty(value) || value.Trim().Length == 0;
		}

		public static string TruncateStart(this string value, int length, string truncation = "...")
		{
			if (value.Length <= length) return value;
			return truncation + value.Substring(value.Length - length);
		}

		// TODO: Convert to use "this string"
		public static string GetNonNullOrEmpty(string value, string defaultValue) => string.IsNullOrEmpty(value) ? defaultValue : value;

		public static string Wrap(
			this string value,
			string begin,
			string end
		) => begin + value + end;

		public static string WrapColor(
			this string value,
			string color
		) => value.Wrap($"<color={color}>", "</color>");
		
		public static string WrapColor(
			this string value,
			Color color
		) => value.Wrap($"<color=#{color.ToHtmlRgba()}>", "</color>");

		/// <summary>
		/// Converts camelCase or PascalCase to lower_snake_case.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static string ToSnakeCase(this string value)
		{
			return string.Concat(
				value.Select((c, i) => 0 < i && char.IsUpper(c) ? "_" + c : c.ToString())
			).ToLower();
		}
		
		/// <summary>
		/// Converts an arbitrary number of camelCase or PascalCase entries to lower.snake_case.
		/// </summary>
		/// <param name="elements"></param>
		/// <returns></returns>
		public static string ToPunctualSnakeCase(
			this IEnumerable<string> elements
		)
		{
			var result = string.Empty;
			var isFirst = true;

			foreach (var element in elements)
			{
				if (isFirst) isFirst = false;
				else result += ".";

				result += element.ToSnakeCase();
			}

			return result;
		}
		
		public static Color ToColor(this string value)
		{
			return Color.cyan.NewH(int.Parse(CreateMd5(value).Substring(0, 2), System.Globalization.NumberStyles.HexNumber) / 255f);
		}
		
		// TODO: Lol hacks.
		static string CreateMd5(string input)
		{
			input = input ?? string.Empty;
			// Use input string to calculate MD5 hash
			using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
			{
				var inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
				var hashBytes = md5.ComputeHash(inputBytes);

				// Convert the byte array to hexadecimal string
				var sb = new System.Text.StringBuilder();
				foreach (var t in hashBytes)
				{
					sb.Append(t.ToString("X2"));
				}
				return sb.ToString();
			}
		}
	}
}