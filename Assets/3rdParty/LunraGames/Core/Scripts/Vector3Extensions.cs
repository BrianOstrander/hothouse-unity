using System;

using UnityEngine;

namespace LunraGames
{
	public static class Vector3Extensions
	{
		public static bool Approximately(this Vector3 vector3, Vector3 other)
		{
			return Mathf.Approximately(vector3.x, other.x) && Mathf.Approximately(vector3.y, other.y) && Mathf.Approximately(vector3.z, other.z);
		}

		public static Vector3 NewX(this Vector3 vector3, float x)
		{
			return new Vector3(x, vector3.y, vector3.z);
		}

		public static Vector3 NewY(this Vector3 vector3, float y)
		{
			return new Vector3(vector3.x, y, vector3.z);
		}

		public static Vector3 NewZ(this Vector3 vector3, float z)
		{
			return new Vector3(vector3.x, vector3.y, z);
		}

		/// <summary>
		/// Flattens the y to zero, and normalizes the result, perfect for an overhead camera
		/// </summary>
		/// <returns>The normalized result.</returns>
		/// <param name="vector3">Vector3.</param>
		public static Vector3 FlattenY(this Vector3 vector3)
		{
			return new Vector3(vector3.x, 0f, vector3.z).normalized;
		}

		public static Vector3[] Add(this Vector3[] vector3s, Vector3 addition)
		{
			var result = new Vector3[vector3s.Length];
			for (var i = 0; i < vector3s.Length; i++) result[i] = vector3s[i] + addition;
			return result;
		}

		public static Vector3 ScaleBy(this Vector3 vector3, Vector3 scaler)
		{
			vector3.Scale(scaler);
			return vector3;
		}

		public static Vector3 ApproximateSubtract(this Vector3 vector3, Vector3 other)
		{
			return new Vector3(
				ApproximateSubtractFloat(vector3.x, other.x),
				ApproximateSubtractFloat(vector3.y, other.y),
				ApproximateSubtractFloat(vector3.z, other.z)
			);
		}

		static float ApproximateSubtractFloat(float float0, float float1)
		{
			return Mathf.Approximately(float0, float1) ? 0f : float0 - float1;
		}

		public static string ToBytesString(this Vector3 vector3)
		{
			var xValue = SingleToBinaryString(vector3.x);
			var yValue = SingleToBinaryString(vector3.y);
			var zValue = SingleToBinaryString(vector3.z);

			return vector3+"\n(\n\t" + xValue + "\n\t" + yValue + "\n\t" + zValue + "\n)";
		}

		public static string SingleToBinaryString(float f)
		{
			byte[] b = BitConverter.GetBytes(f);
			int i = BitConverter.ToInt32(b, 0);
			return Convert.ToString(i, 2);
		}
	}
}