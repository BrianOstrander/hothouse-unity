using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public static class LayerNames
	{
		public const string Floor = "Floor";
		public const string Unexplored = "Unexplored";
	}

	public static class LayerMasks
	{
		public static readonly int Floor = LayerMask.GetMask(LayerNames.Floor);
	}
}