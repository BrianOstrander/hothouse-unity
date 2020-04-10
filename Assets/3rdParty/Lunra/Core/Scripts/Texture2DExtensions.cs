using UnityEngine;

namespace LunraGames
{
	public static class Texture2DExtensions
	{
		public static Color[][] GetAsArray(this Texture2D texture)
		{
			if (texture == null) return null;
			var width = texture.width;
			var height = texture.height;

			var result = new Color[width][];
			var colors = texture.GetPixels();

			for (var x = 0; x < width; x++)
			{
				result[x] = new Color[height];
				for (var y = 0; y < height; y++)
				{
					result[x][y] = colors[PixelCoordinateToIndex(x, y, width, height)];
				}
			}
			return result;
		}

		/// <summary>
		/// PixelArray[0] of an image is the lower left pixel, but the Pixel2D[0,0] is the upper left. This translates the (x, y) coordinate to the proper index.
		/// </summary>
		/// <returns>The index of the specified pixel at (x, y).</returns>
		/// <param name="x">The x coordinate of the pixel.</param>
		/// <param name="y">The y coordinate of the pixel.</param>
		/// <param name = "width">The width of the texture.</param>
		/// <param name = "height">The height of the texture.</param>
		public static int PixelCoordinateToIndex(int x, int y, int width, int height)
		{
			return ((width * height) - 1) - ((width * y) + x);
		}
	}
}