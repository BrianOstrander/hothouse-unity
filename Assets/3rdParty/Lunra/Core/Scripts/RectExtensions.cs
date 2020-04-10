using UnityEngine;

namespace LunraGames
{
	public static class RectExtensions
	{
		public static Rect NewX(this Rect rect, float x)
		{
			return new Rect(x, rect.y, rect.width, rect.height);
		}

		public static Rect NewY(this Rect rect, float y)
		{
			return new Rect(rect.x, y, rect.width, rect.height);
		}

		public static Rect NewWidth(this Rect rect, float width)
		{
			return new Rect(rect.x, rect.y, width, rect.height);
		}

		public static Rect NewHeight(this Rect rect, float height)
		{
			return new Rect(rect.x, rect.y, rect.width, height);
		}
	}
}