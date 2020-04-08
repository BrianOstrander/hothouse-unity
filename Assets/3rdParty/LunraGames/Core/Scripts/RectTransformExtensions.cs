using UnityEngine;

namespace LunraGames
{
	public static class RectTransformExtensions
	{
		static Vector3 WorldCorner(this RectTransform transform, int corner)
		{
			Vector3[] corners = new Vector3[4];
			transform.GetWorldCorners(corners);
			return corners[corner];
		}

		public static Vector3 MinWorldCorner(this RectTransform transform)
		{
			return transform.WorldCorner(1);
		}

		public static Vector3 MaxWorldCorner(this RectTransform transform)
		{
			return transform.WorldCorner(3);
		}

		public static void MinMaxWorldCorner(this RectTransform transform, out Vector3 min, out Vector3 max)
		{
			min = transform.WorldCorner(1);
			max = transform.WorldCorner(3);
		}

		/// <summary>
		/// Gets the distance between the top left and bottom right corners in world space.
		/// </summary>
		/// <returns>The corner size.</returns>
		/// <param name="transform">Transform.</param>
		public static Vector3 WorldCornerSize(this RectTransform transform)
		{
			Vector3 min;
			Vector3 max;
			transform.MinMaxWorldCorner(out min, out max);
			return max - min;
		}
	}
}